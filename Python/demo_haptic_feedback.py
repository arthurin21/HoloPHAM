import numpy as np

from HapticController import HapticController
from HoloTactilePercepts import HoloTactilePercepts

HAPTIC_MAC = 'dc:94:6c:bb:ea:a3'
HOLOLENS_IP = '127.0.0.1'

TACTILE_PORT = 9028
NUM_TACTILE_SENSORS = 5

if __name__ == '__main__':
    print( "Creating haptic interface...", end = '', flush = True )
    haptic = HapticController( mac = HAPTIC_MAC )
    print( 'Done!' )

    print( "Creating hololens tactile percepts interface...", end = '', flush = True )
    percepts = HoloTactilePercepts( ip = HOLOLENS_IP, port = TACTILE_PORT, num_sensors = NUM_TACTILE_SENSORS )
    print( 'Done!' )

    try:
        while True:
            state = percepts.state
            if state is not None:
                vibrate = np.round( 255 * state / 50.0 )
                print( 'Vibration Values:', vibrate )
                haptic.publish( vibrate )
    finally:
        haptic.close()

