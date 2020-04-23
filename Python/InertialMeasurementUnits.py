import time
import serial
import struct
import ctypes
import queue

import numpy as np
import numpy.matlib

import multiprocessing as mp
import matplotlib.pyplot as plt
import mpl_toolkits.mplot3d as plt3d
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

# from .. import ns_sleep
import Quaternion as quat

class InertialMeasurementUnits:
    """ Python implementation of the inertial measurement unit net """
    MAX_INIT_RETRIES = 10

    def __init__(self, name = 'IMU', com = '/dev/ttyACM0', baud = 115200, chan = [ 0, 1, 2, 3, 4 ], srate = 50.0):
        """ 
        Constructor 
        
        Parameters
        ----------
        name : str
            The unique device handle used to refer to the connected hardware
        com : str
            The named communication port for the serial connection (e.g. COM9 on Windows, /dev/ttyACM1 on linux)
        baud : int
            The communication baudrate for the IMU dongle (should be 115200)
        chan : iterable of ints
            The IMU peripheral IDs that should be attached
        srate : float
            The sampling rate used to poll the device

        Returns
        -------
        obj
            An InertialMeasurementUnits interface object
        """
        # interface variables
        self._name = name
        self._channels = chan
        self._channelcount = len( chan )
        self._speriod = 1.0 / srate

        # device variables
        self._state = np.zeros( ( 4 * self._channelcount, ) )
        self._calibrate = mp.Array( ctypes.c_float, 4 * self._channelcount )
        
        # initialize arrays
        self._state[:] = np.squeeze( np.matlib.repmat( np.array( [ 1, 0, 0, 0 ], dtype = np.float ), 1, self._channelcount ) )
        self._calibrate[:] = np.squeeze( np.matlib.repmat( np.array( [ 1, 0, 0, 0 ], dtype = np.float ), 1, self._channelcount ) )

        # state variables
        self._state_buffer = mp.Queue()
        self._state_buffer_max = int( 5 * srate )

        # streaming variables
        self._exit_event = mp.Event()
        self._conn_event = mp.Event()
        self._stream_event = mp.Event()
        self._print_event = mp.Event()

        # viewing variables
        self._view_event = mp.Event()
        self._plot_buffer = mp.Queue()
        self._viewer = None

        self._ser = None
        self._streamer = mp.Process( target = self._connect, args = ( com, baud ) )
        self._streamer.start()
        self._conn_event.wait()

    def __del__(self):
        """ 
        Destructor
        
        This cleans up any spawned child processes / resources for the interface
        """
        try:
            if self._streamer.is_alive: 
                self._stream_event.clear()
                self._exit_event.set()
                # self._streamer.join()
        except AttributeError: pass # never got to make the I/O process
        try:
            if self._viewer.is_alive: self.hide()
        except AttributeError: pass # no viewer exists currently

    def _set_channels(self):
        """ 
        Sets active channels for the dongle
        
        Returns
        -------
        bool
            True if channels are properly set, False else
        """
        mask = b'\x80\x40\x20\x10\x08\x04\x02\x01'
        cmd = bytearray(b'\x63\x00')
        for i in self._channels:
            cmd[1] = cmd[1] | mask[i]
        self._ser.write(cmd)
        ch = self._ser.read(1)
        if len( ch ):
            ch = struct.unpack('B', ch)[0]
            return (ch == len(self._channels))
        else: return False

    def _query_init(self):
        """ 
        Checks for proper initialization
        
        Returns
        -------
        bool
            True if each IMU was properly initialized, False else
        """
        for _ in range(0, InertialMeasurementUnits.MAX_INIT_RETRIES):
            self._ser.write(b'\x69')
            init = self._ser.read(1)
            init = struct.unpack('B', init)[0]
            if init == 121: return True
            time.sleep( 0.2 ) # wait a second
        return False
    
    def _chksum(self, b):
        """
        Verifies data communication integrity by comparing a checksum of the data with the last byte

        Parameters
        ----------
        b : bytes
            Incoming data with a checksum byte appended

        Returns
        -------
        bool
            True if checksum matches, False else
        """
        return ( ( sum(bytearray(b[:-1])) % 256 ) == b[-1] )

    def _connect(self, com, baud):
        """
        Connect to specified IMU devices

        Parameters
        ----------
        com : str
            The serial port that the IMU is connected to
        baud : int
            The communication baudrate for the serial device

        Raises
        ------
        RuntimeError
            If the IMU was not initialized correctly
        ValueError
            If the IMU channels were not set correctly

        Notes
        -----
        This function is the target of the the child polling process
        """
        try:
            self._ser = serial.Serial(com, baud, timeout = 0.5) # connect to serial device
            if self._set_channels():                            # set up channel information
                if self._query_init():                          # query initialization success
                    self._conn_event.set()                      # signal connection

                    # streaming
                    while not self._exit_event.is_set():
                        if self._stream_event.is_set():
                            self._stream()
        
                else:
                    raise RuntimeError("IMU was not initialized correctly")
            else:
                raise ValueError("IMU channels were not set correctly", self._channels )
        finally:
            # close serial port (if we opened it)
            if self._ser is not None and self._ser.is_open:
                self._ser.close()
            
            # drain all processing queues so we can join
            self.flush()
            empty = False
            while not empty:
                try: self._plot_buffer.get( timeout = 1e-3 )
                except queue.Empty: empty = True
                self._conn_event.set()

            # signal connection so main process isn't held up
            self._conn_event.set()

    def _read(self):
        """ 
        Reads a single sample from the IMUs

        While this function does not return anything, it sets the __state variable
        to the last measured sensor readings.
        """
        self._ser.write(b'\x77')
        sample = self._ser.read( 16 * self._channelcount + 1 )
        if self._chksum(sample):
            data = np.array(struct.unpack(4*self._channelcount*'f', sample[0:-1]))
            for i in range( 0, self._channelcount):
                idx1 = i * 4
                idx2 = idx1 + 4
                self._state[idx1:idx2] = quat.relative( np.array( self._calibrate[idx1:idx2] ), data[idx1:idx2] )
            
            while self._state_buffer.qsize() > self._state_buffer_max: 
                self._state_buffer.get( timeout = 1e-3 )
            self._state_buffer.put( self._state.copy(), timeout = 1e-3 )
            
            if self._print_event.is_set(): print( self._name, ':', self._state )
            if self._view_event.is_set(): self._plot_buffer.put( self._state )
        else:
            self._ser.flushInput()
    
    def _stream(self):
        """ 
        Streams data from the IMUs at the specified sampling rate 

        Notes
        -----
        This function is the target of the the child polling process
        """
        t = time.time()
        while self._stream_event.is_set():
            t = t + self._speriod
            self._read()
            while max( t - time.time(), 0 ): time.sleep( 0.001 ) # ns_sleep( 1e2 )

    def _plot(self):
        """
        Generates a visualization of the incoming data in real-time
        """
        # initialization
        cube_x = np.array( [ [ 0, 1, 1, 0, 0, 0 ], [ 1, 1, 0, 0, 1, 1 ],
                             [ 1, 1, 0, 0, 1, 1 ], [ 0, 1, 1, 0, 0, 0 ] ] ) - 0.5
        cube_y = np.array( [ [ 0, 0, 1, 1, 0, 0 ], [ 0, 1, 1, 0, 0, 0 ],
                             [ 0, 1, 1, 0, 1, 1 ], [ 0, 0, 1, 1, 1, 1 ] ] ) - 0.5
        cube_z = np.array( [ [ 0, 0, 0, 0, 0, 1 ], [ 0, 0, 0, 0, 0, 1 ],
                             [ 1, 1, 1, 1, 0, 1 ], [ 1, 1, 1, 1, 0, 1 ] ] ) - 0.5
        cube_colors = 'rgbycm'
                
        n_rows = np.ceil( np.sqrt( self._channelcount ) )
        n_cols = np.ceil( np.sqrt( self._channelcount ) )
                
        gui = plt.figure()
        gui.canvas.set_window_title( self._name )
        
        polygons = []
        for i in range( 0, self._channelcount ):
            polygons.append( [] )
            ax = gui.add_subplot( n_rows, n_cols, i + 1,
                                  projection = '3d' ) #, aspect = 'equal' )

            for side in range( 0, 6 ):
                vtx = np.array( [ cube_x[:, side],
                                  cube_y[:, side],
                                  cube_z[:, side] ] )
                poly = plt3d.art3d.Poly3DCollection( [ np.transpose( vtx ) ] )
                poly.set_color( cube_colors[ side ] )
                poly.set_edgecolor( 'k' )
                polygons[ i ].append( poly )
                
                ax.add_collection3d( polygons[ i ][ side ] )
            ax.set_xlim( ( -1, 1 ) )
            ax.set_ylim( ( -1, 1 ) )
            ax.set_zlim( ( -1, 1 ) )
            ax.set_title( 'IMU #' + repr( self._channels[ i ] + 1 ) )
            ax.axis( 'off' )

        # stream
        plt.tight_layout()
        plt.show( block = False )
        xnew = np.zeros( ( 4, 6 ) )
        ynew = np.zeros( ( 4, 6 ) )
        znew = np.zeros( ( 4, 6 ) )
        while self._view_event.is_set():
            try:
                data = None
                while self._plot_buffer.qsize() > 0: data = self._plot_buffer.get()
                if data is not None:
                    for dev in range(0, self._channelcount):
                        idx1 = dev * 4
                        idx2 = idx1 + 4
                        q = data[idx1:idx2]
                        for j in range( 0, 6 ):
                            for i in range( 0, 4 ):
                                p = np.array( [ cube_x[ i, j ], 
                                                cube_y[ i, j ],
                                                cube_z[ i, j ] ] )
                                pr = quat.rotate( q, p )
                                xnew[ i, j ] = pr[ 0 ]
                                ynew[ i, j ] = pr[ 1 ]
                                znew[ i, j ] = pr[ 2 ]
                            vtx = np.array( [ xnew[:, j], ynew[:, j], znew[:, j] ] )
                            polygons[ dev ][ j ].set_verts( [ np.transpose( vtx ) ] )
                plt.pause( 0.05 )
            except: self._view_event.clear()
        plt.close( gui )

    @property
    def name(self):
        """
        The unique handle for the device
        
        Returns
        -------
        str
            The specified unique handle of this device interface
        """ 
        return self._name

    @property
    def state(self):
        """
        The current state for the device
        
        Returns
        -------
        numpy.ndarray (n_channels,)
            The last measured sensor readings
        """
        try:
            return self._state_buffer.get( timeout = 1e-3 )
        except queue.Empty:
            return None

    @property
    def speriod(self): 
        """
        The sampling period for the device
        
        Returns
        -------
        float
            The sampling period of the device
        """
        return self._speriod

    @property
    def channelcount(self):
        """
        The channel count for the device

        Returns
        -------
        int
            The number of channels per sensor measurement
        """
        return self._channelcount

    def set_calibrate(self, calibration_count = 100):
        """ 
        Set the calibration quaternions for the IMUs 
        
        Parameters
        ----------
        calibration_count : int
            The number of quaternions to sample to get a baseline
        """
        self.clear_calibrate()          # clear calibration

        streaming = self._stream_event.is_set() 
        if not streaming:
            self.run( display = False ) # start data collection
        
        # collect samples
        Q = []
        count = 0
        while count < calibration_count:
            q = self.state
            if q is not None:
                Q.append( q )
                count += 1

        # compute average of collected samples
        Q = np.vstack( Q ).T # quaternions x samples
        for i in range( 0, self._channelcount ):
            qidx = 4 * i
            self._calibrate[qidx:qidx+4] = quat.average( Q[qidx:qidx+4,:] )

        # reset streaming if we are not originally
        if not streaming:
            self.stop()
            self.flush()

    def clear_calibrate(self):
        """
        Clear the calibration quaternions for the IMUs
        """
        self._calibrate[:] = np.squeeze( np.matlib.repmat( np.array( [ 1, 0, 0, 0 ] ), 1, self._channelcount ) )

    def get_calibrate(self):
        """
        Get the current calibration quaternions for the IMUs

        Returns
        -------
        numpy.ndarray (n_channels x 4,)
            The calibration orientations for each sensor
        """
        return np.frombuffer( self._calibrate.get_obj(), np.float32 ).copy()

    def run(self, display = False):
        """ 
        Starts the acquisition process of the IMUs

        Parameters
        ----------
        display : bool
            Flag determining whether sensor measurements should be printed to console (True) or not (False)
        """
        if not self._stream_event.is_set():
            self._stream_event.set()

            if display: self._print_event.set()
            else: self._print_event.clear()

    def flush(self):
        """
        Dispose of all previous data
        """
        empty = False
        while not empty:
            try: self._state_buffer.get( timeout = 1e-3 )
            except queue.Empty: empty = True

    def stop(self):
        """
        Stops the acquisition process of the IMUs
        """
        if self._stream_event.is_set():
            self._stream_event.clear()

    def view(self):
        """ 
        Launches the GUI viewer of the IMU data 
        """
        if not self._view_event.is_set():
            self._view_event.set()
            self._viewer = mp.Process( target = self._plot )
            self._viewer.start()
            
    def hide(self):
        """ 
        Closes the GUI viewer of the IMU data
        """
        if self._view_event.is_set():
            self._view_event.clear()
            self._viewer.join()

if __name__ == '__main__':
    import sys
    import inspect
    import argparse

    # helper function for booleans
    def str2bool( v ):
        if v.lower() in [ 'yes', 'true', 't', 'y', '1' ]: return True
        elif v.lower() in [ 'no', 'false', 'n', 'f', '0' ]: return False
        else: raise argparse.ArgumentTypeError( 'Boolean value expected!' )

    # parse commandline entries
    class_init = inspect.getargspec( InertialMeasurementUnits.__init__ )
    arglist = class_init.args[1:]   # first item is always self
    defaults = class_init.defaults
    parser = argparse.ArgumentParser()
    for arg in range( 0, len( arglist ) ):
        try: tgt_type = type( defaults[ arg ][ 0 ] )
        except: tgt_type = type( defaults[ arg ] )
        if tgt_type is bool:
            parser.add_argument( '--' + arglist[ arg ], 
                             type = str2bool, nargs = '?',
                             action = 'store', dest = arglist[ arg ],
                             default = defaults[ arg ] )
        else:
            parser.add_argument( '--' + arglist[ arg ], 
                                type = tgt_type, nargs = '+',
                                action = 'store', dest = arglist[ arg ],
                                default = defaults[ arg ] )
    args = parser.parse_args()
    for arg in range( 0, len( arglist ) ):
        attr = getattr( args, arglist[ arg ] )
        if isinstance( attr, list ) and not isinstance( defaults[ arg ], list ):
            setattr( args, arglist[ arg ], attr[ 0 ]  )

    # create interface

    imu = InertialMeasurementUnits( name = args.name, com = args.com, baud = args.baud, chan = args.chan, srate = args.srate )
    imu.set_calibrate()
    
    imu.run( display = False )
    imu.view()

    while True:
        state = imu.state
        if state is not None:
            print( state )