import time
import queue
import socket
import struct

import numpy as np
import multiprocessing as mp

class HoloTactilePercepts:
    def __init__(self, ip = '127.0.0.1', port = 9028, num_sensors = 5):
        self._ip_addr = ( ip, port )
        self._channelcount = num_sensors
        
        self._state = np.zeros( ( self._channelcount, ) )
        self._state_buffer = mp.Queue()

        # synchronization variables
        self._exit_event = mp.Event()
        self._connect_event = mp.Event()

        # start streaming process
        self._streamer = mp.Process( target = self._connect )
        self._streamer.start()

        self._connect_event.wait()

    def __del__(self):
        pass
        # if self._streamer.is_alive:
        #     self._exit_event.set()
        #     self._streamer.join()


    def _connect(self):
        # create socket interface
        try:
            sock = socket.socket( socket.AF_INET, socket.SOCK_DGRAM )
            sock.bind( self._ip_addr )
            sock.setblocking( False )
        finally:
            self._connect_event.set()

        # stream data while not exiting
        try:
            while not self._exit_event.is_set():
                # pull data from socket
                try:
                    data = sock.recv( 4 * self._channelcount )
                    self._state[:] = struct.unpack( '<' + self._channelcount * 'f', data )

                    # push data into state buffer
                    while self._state_buffer.qsize() > 100:
                        self._state_buffer.get( timeout = 1e-3 )
                    self._state_buffer.put( self._state.copy(), timeout = 1e-3 )
                except BlockingIOError:
                    pass
        finally:
            # close the socket before we exit
            sock.close()

    @property
    def channelcount(self):
        return self._channelcount

    @property
    def state(self):
        try:
            return self._state_buffer.get( timeout = 1e-3 )
        except queue.Empty:
            return None

if __name__ == "__main__":
    import sys
    import inspect
    import argparse

    # helper function for booleans
    def str2bool( v ):
        if v.lower() in [ 'yes', 'true', 't', 'y', '1' ]: return True
        elif v.lower() in [ 'no', 'false', 'n', 'f', '0' ]: return False
        else: raise argparse.ArgumentTypeError( 'Boolean value expected!' )

    # parse commandline entries
    class_init = inspect.getargspec( HoloTactilePercepts.__init__ )
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

    percepts = HoloTactilePercepts( ip = args.ip, port = args.port, num_sensors = args.num_sensors )
    print( "Hololens tactile percepts interface created." )

    while True:
        state = percepts.state
        if state is not None:
            print( state )