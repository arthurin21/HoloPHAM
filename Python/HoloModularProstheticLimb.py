import struct
import socket

class HoloModularProstheticLimb:
    UDP_SERVER_ADDR = ( '127.0.0.1', 10000 )
    UDP_JOINT_START_BYTE = b'\x6a'

    NUM_JOINT_ANGLES = 27

    def __init__( self, ip = '127.0.0.1', port = 9027 ):
        """
        Constructor

        Parameters
        ----------
        ip : str
            The remote IP of the Hololens
        port : int
            The remote port that the Hololens is listening on
        """
        # create UDP socket
        self._udp_tx = ( ip, port )
        self._sock = socket.socket( socket.AF_INET, socket.SOCK_DGRAM )
        self._sock.bind( HoloModularProstheticLimb.UDP_SERVER_ADDR )
        self._sock.setblocking( False )

        self._last_angles = [ 0.0 for _ in range( HoloModularProstheticLimb.NUM_JOINT_ANGLES ) ]

    def publish( self, move = None, joint_angles = None ):
        """
        Parameters
        ----------
        move : str or None
            A movement class id
        joint_angles : iterable of floats (n_joints,)
            Joint angles to send to each joint
        """
        if joint_angles is not None: angles = joint_angles
        else: angles = self._last_angles

        if move is not None:
            try:
                pass # overwrite relevant joints for THIS class
            except KeyError:
                pass

        # send data over UDP
        fmt = '<c' + HoloModularProstheticLimb.NUM_JOINT_ANGLES * 'f'
        data = struct.pack( fmt, HoloModularProstheticLimb.UDP_JOINT_START_BYTE, *angles )
        self._sock.sendto( data, self._udp_tx )

        # store this configuration
        self._last_angles = angles

if __name__ == '__main__':
    import sys
    import inspect
    import argparse

    import time
    import numpy as np

    # helper function for booleans
    def str2bool( v ):
        if v.lower() in [ 'yes', 'true', 't', 'y', '1' ]: return True
        elif v.lower() in [ 'no', 'false', 'n', 'f', '0' ]: return False
        else: raise argparse.ArgumentTypeError( 'Boolean value expected!' )

    # parse commandline entries
    class_init = inspect.getargspec( HoloModularProstheticLimb.__init__ )
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
    holo = HoloModularProstheticLimb( ip = args.ip, port = args.port )

    # create data
    t = np.linspace( 0, 2 * np.pi, 20 )
    angles = 180.0 * np.abs( np.vstack( [ np.sin( t ) for _ in range( HoloModularProstheticLimb.NUM_JOINT_ANGLES ) ] ).T )

    # send data over
    for i in range( angles.shape[0] ):
        print( '(%d) Sending Angles:' % (i+1), angles[i,0] )
        holo.publish( move = None, joint_angles = angles[i,:] )
        time.sleep( 1 )