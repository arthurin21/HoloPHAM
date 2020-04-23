import serial
import struct
import ctypes
import time
import queue

import numpy as np
import multiprocessing as mp

class CyberGlove:
    """ Python implementation of a Wired CyberGlove driver  """
    def __init__(self, name = 'CyberGlove', com = '/dev/ttyUSB0', baud = 115200, srate = 50.0 ):
        """ 
        Constructor

        Parameters
        ----------
        name : str
            The unique device handle used to refer to the connected hardware
        com : str
            The named communication port for the serial connection (e.g. COM9 on Windows, /dev/ttyUSB0 on linux)
        baud : int
            The communication baudrate for the CyberGlove (should be 115200)
        srate : float
            The sampling rate used to poll the device (maximum is 90 Hz)

        Returns
        -------
        obj
            A CyberGlove interface object
        
        Raises
        ------
        ValueError
            If the CyberGlove returns an invalid channel count
        RuntimeError
            If the CyberGlove was not initialized correctly
        """
        # interface variables
        self._name = name
        self._speriod = 1.0 / srate

        # we need to query the CyberGlove quickly
        self._ser = serial.Serial( com, baud, timeout = 0.5 )
        if self._query_init():
            ch = self._query_channels()
            if ch > 0:
                self._channelcount = ch
            else:
                raise ValueError("CyberGlove channel count must be positive", 'ch')
        else:
            raise RuntimeError("CyberGlove was not initialized correctly")

        # release serial resource so child process can own it
        if self._ser.is_open: self._ser.close()
        self._ser = None

        # state variables
        self._state = np.zeros( ( self._channelcount, ) )
        self._state_buffer = mp.Queue()
        self._state_buffer_max = int( 5 * srate )

        self._calibrate = mp.Array( ctypes.c_double, 2 * self._channelcount )
        self._calibrate[:self._channelcount] = 255 * np.ones( self._channelcount )

        # streaming variables
        self._exit_event = mp.Event()
        self._conn_event = mp.Event()
        self._stream_event = mp.Event()
        self._print_event = mp.Event()

        # viewing variables
        self._view_event = mp.Event()
        self._plot_buffer = mp.Queue()
        self._viewer = None

        # connect to hardware
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
                # print( '', end = '', flush = True ) # TODO: WHY DOES THIS WORK?
                self._stream_event.clear()
                self._exit_event.set()
                # self._streamer.join()
        except AttributeError: pass # never got to make the I/O process
        try:
            if self._viewer.is_alive: self.hide()
        except AttributeError: pass # no viewer exists currently

    def _query_init(self):
        """ 
        Checks for proper device initialization

        Returns
        -------
        bool
            True if properly initialized, False else
        """
        self._ser.write(b'\x3F\x47')
        init = self._ser.read(4)
        if len(init) < 4: return False
        else: return (init[2] == 3)

    def _query_channels(self):
        """ 
        Checks for number of channels the CyberGlove has 
        
        Returns
        -------
        int
            Number of CyberGlove sensors
        """
        self._ser.write(b'\x3F\x53')
        ch = self._ser.read( 4 )
        if len(ch) < 4: return 0
        else: return int( ch[2] )

    def _connect(self, com, baud):
        """
        Connect to specified CyberGlove device

        Parameters
        ----------
        com : str
            The serial port that the CyberGlove is connected to
        baud : int
            The communication baudrate for the serial device

        Raises
        ------
        RuntimeError
            If the CyberGlove was not initialized correctly

        Notes
        -----
        This function is the target of the the child polling process
        """
        try:
            self._ser = serial.Serial(com, baud, timeout = 0.5) # connect to serial device
            if self._query_init():                              # query initialization success
                self._conn_event.set()                          # signal connection
                
                # streaming
                while not self._exit_event.is_set():
                    if self._stream_event.is_set():
                        self._stream()
            else: 
                raise RuntimeError("CyberGlove was not initialized correctly")
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
            
            # signal connection so main process isn't held up
            self._conn_event.set()

    def _read(self):
        """ 
        Reads a single sample from the CyberGlove

        While this function does not return anything, it sets the _state variable
        to the last measured sensor readings.
        
        Notes
        -----
        For the Wired CyberGlove, channels correspond to the following sensors:
        00 Thumb MCP   01 Thumb PIP    02 Thumb DIP   03 Thumb ABD
        04 Index MCP   05 Index PIP    06 Index DIP   07 Index ABD
        08 Middle MCP  09 Middle PIP   10 Middle DIP  11 Middle ABD
        12 Ring MCP    13 Ring PIP     14 Ring DIP    15 Ring ABD
        16 Pinky MCP   17 Pinky PIP    18 Pinky DIP   19 Pinky ABD
        20 Palm Arch   21 Wrist Pitch  22 Wrist Yaw
        """
        self._ser.write(b'\x47')
        sample = self._ser.read( self._channelcount + 2 )
        values = np.array( struct.unpack( self._channelcount*'B', sample[1:-1] ) )

        vmin = np.array( self._calibrate[ -self._channelcount: ] )
        vmax = np.array( self._calibrate[ :self._channelcount ] )
        
        self._state[:] = np.divide( values - vmin, vmax - vmin )
        while self._state_buffer.qsize() > self._state_buffer_max: 
            self._state_buffer.get( timeout = 1e-3 )
        self._state_buffer.put( self._state.copy(), timeout = 1e-3 )

        if self._print_event.is_set(): print( self._name, ':', self._state )
        if self._view_event.is_set(): self._plot_buffer.put( self._state )

    def _stream(self):
        """ 
        Streams data from the CyberGlove at the specified sampling rate
        """
        t = time.time()
        while self._stream_event.is_set():
            t = t + self._speriod
            self._read()
            while max( t - time.time(), 0 ): time.sleep( 0.001 )

    def _plot(self):
        """
        Generates a visualization of the incoming data in real-time
        """
        self._view_event.clear()

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

    def set_calibrate(self, timeout = 10):
        """
        Sets the calibration values for the CyberGlove

        Calibration aims to record the maximum and minimum values of each sensor.
        These values are then used to scale subsequent readings between [0, 1].

        Parameters
        ----------
        timeout : float
            The amount of time (in seconds) to collect calibration data
        """
        streaming = self._stream_event.is_set()
        if not streaming: 
            self.run( display = False ) # start data collection

        # compute these to undo calibration
        vmin = np.array( self._calibrate[ -self._channelcount: ] )
        vmax = np.array( self._calibrate[ :self._channelcount ] )

        # collect samples
        samples = []
        t0 = time.perf_counter()
        while time.perf_counter() - t0 < timeout:
            sample = self.state
            if sample is not None: 
                samples.append( sample * ( vmax - vmin ) + vmin )
        samples = np.vstack( samples )
        self._calibrate[:self._channelcount] = np.amax( samples, axis = 0 )
        self._calibrate[-self._channelcount:] = np.amin( samples, axis = 0 )

        # check calibration range
        equal = np.equal( self._calibrate[:self._channelcount], self._calibrate[-self._channelcount:] )
        for i in range( self._channelcount ):
            if equal[i]:    # both min and max are the same
                if self._calibrate[i] > 0: self._calibrate[i] -= 1
                elif self._calibrate[-i] < 255: self._calibrate[-i] += 1

        # reset streaming if we were not originally
        if not streaming:
            self.stop()
            self.flush()

    def clear_calibrate(self):
        """
        Clears the calibration values for this CyberGlove
        """
        self._calibrate[ -self._channelcount: ] = np.zeros( self._channelcount )
        self._calibrate[ :self._channelcount ] = 255 * np.ones( self._channelcount )

    def get_calibrate(self):
        """
        Get the current calibration values for this CyberGlove

        Returns
        -------
        numpy.ndarray (n_channels,)
            The maximum values for each sensor
        numpy.ndarray (n_channels,)
            The minimum values for each sensor
        """
        cal = np.frombuffer( self._calibrate.get_obj() )
        cal = np.reshape( cal, ( 2, self._channelcount ) )
        return cal[0,:], cal[1,:]

    def run(self, display = False):
        """ 
        Starts the acquisition process of the CyberGlove 
        
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
        Stops the acquisition process of the CyberGlove 
        """
        if self._stream_event.is_set():
            self._stream_event.clear()

    def view(self):
        """ 
        Launches the GUI viewer of the CyberGlove data 
        
        Notes
        -----
        This is currently not implemented
        """
        if not self._view_event.is_set():
            self._view_event.set()
            self._viewer = mp.Process( target = self._plot )
            self._viewer.start()
            
    def hide(self):
        """ 
        Closes the GUI viewer of the CyberGlove data 
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
    class_init = inspect.getargspec( CyberGlove.__init__ )
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

    cg = CyberGlove( name = args.name, com = args.com, baud = args.baud, srate = args.srate )
    cg.run( display = False )
    # cg.view()

    while True:
        state = cg.state
        if state is not None:
            print( state )