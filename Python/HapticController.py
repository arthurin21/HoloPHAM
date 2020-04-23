import asyncio
import threading as th
from bleak import BleakClient

class HapticController:
    """ Haptic controller """
    NUM_MOTORS = 5


    UART_SERVICE = "6e400001-b5a3-f393-e0a9-e50e24dcca9e"
    UART_RX_CHAR = "6e400002-b5a3-f393-e0a9-e50e24dcca9e"
    UART_TX_CHAR = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"
    
    UART_PACKET_START = 33

    def __init__(self, mac = 'dc:94:6c:bb:ea:a3'):
        """
        Constructor

        Parameters
        ----------
        mac : str
            The MAC address of the BTLE client

        Returns
        -------
        obj
            A HapticController interface object
        """
        self._mac = mac
        self._output = []
        self._loop = asyncio.get_event_loop()
        
        # multithreading variables
        self._thread = th.Thread( target = self._connect )
        self._connect_event = th.Event()
        self._exit_event = th.Event()

        # connect synchronization
        self._thread.start()
        self._connect_event.wait()

    def __del__(self):
        """
        Destructor

        Stops motors in haptic device and closes communication.
        
        NOTE: THIS IS NOT BEING CALLED
        """
        if self._thread.is_alive():
            self._exit_event.set()
            self._thread.join()

    def _connect( self ):
        """
        Synchronous wrapper to connect to BTLE device
        """
        self._loop.run_until_complete( self._send() )

    async def _send( self ):
        """
        Asynchronous function that sends data over BTLE

        Notes
        -----
        Data is updated by appending to output buffer in the main thread
        """
        async with BleakClient( self._mac, loop = self._loop ) as client:
            self._connect_event.set()
            while not self._exit_event.is_set():
                if len( self._output ):
                    data = self._output.pop( 0 )
                    await client.write_gatt_char( HapticController.UART_RX_CHAR, bytearray( data ), response = False )

            # freeze all haptic outputs before disconnecting
            end_packet = [ HapticController.UART_PACKET_START ] + [ 0 for _ in range( HapticController.NUM_MOTORS ) ]
            end_packet += [ sum( end_packet ) % 256 ]
            await client.write_gatt_char( HapticController.UART_RX_CHAR, bytearray( end_packet ), response = False )

    def publish(self, msg):
        """
        Publish output to the haptic controller

        Parameters
        ----------
        msg : iterable of int [0, 255], size = n_motors
            The motor values for

        Raises
        ------
        RuntimeWarning
            Number of driven outputs does not match number of motors in the interface
        """
        if len( msg ) == HapticController.NUM_MOTORS:
            packet = [ HapticController.UART_PACKET_START ] + [ min( max( int( x ), 0 ), 255 ) for x in msg ]
            packet += [ sum( packet ) % 256 ]
            self._output.append( packet )
        else:
            raise RuntimeWarning( "Number of outputs (%d) does not match motor count (%d)!" % ( len( msg ), HapticController.NUM_MOTORS ) )

    def close( self ):
        if self._thread.is_alive():
            self._exit_event.set()
            self._thread.join()

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
    class_init = inspect.getargspec( HapticController.__init__ )
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

    haptic = HapticController( args.mac )

    done = False
    while not done:
        cmd = input( 'Motor Strength [0 - 255]: ' )
        if cmd.lower() == 'q':
            done = True
        else:
            values = [ int( cmd ) for _ in range( HapticController.NUM_MOTORS ) ]
            haptic.publish( values )
    print( 'Bye-bye!' )

    haptic.close()  # TODO: Figure out how to make this unnecessary