import numpy as np
import numpy.linalg as la

# axis sequences for Euler angles
_NEXT_AXIS = [1, 2, 0, 1]

# map axes strings to/from tuples of inner axis, parity, repetition, frame
_AXES2TUPLE = {
    'sxyz': (0, 0, 0, 0), 'sxyx': (0, 0, 1, 0), 'sxzy': (0, 1, 0, 0),
    'sxzx': (0, 1, 1, 0), 'syzx': (1, 0, 0, 0), 'syzy': (1, 0, 1, 0),
    'syxz': (1, 1, 0, 0), 'syxy': (1, 1, 1, 0), 'szxy': (2, 0, 0, 0),
    'szxz': (2, 0, 1, 0), 'szyx': (2, 1, 0, 0), 'szyz': (2, 1, 1, 0),
    'rzyx': (0, 0, 0, 1), 'rxyx': (0, 0, 1, 1), 'ryzx': (0, 1, 0, 1),
    'rxzx': (0, 1, 1, 1), 'rxzy': (1, 0, 0, 1), 'ryzy': (1, 0, 1, 1),
    'rzxy': (1, 1, 0, 1), 'ryxy': (1, 1, 1, 1), 'ryxz': (2, 0, 0, 1),
    'rzxz': (2, 0, 1, 1), 'rxyz': (2, 1, 0, 1), 'rzyz': (2, 1, 1, 1) }

_TUPLE2AXES = dict((v, k) for k, v in _AXES2TUPLE.items())

_EPS4 = np.finfo(float).eps * 4

def magnitude( q ):
    """
    Computes the magnitude of the quaternion
    
    Parameters
    ----------
    q : numpy.ndarray (4,)
        Input quaternion

    Returns
    -------
    float
        The magnitude of the quaternion
    """
    return np.sqrt( np.sum( np.square( q ) ) )

def normalize( q ):
    """
    Computes the normalized quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to normalize
    
    Returns
    -------
    numpy.ndarray (4,)
        The normalized quaternion
    """
    return np.divide( q, magnitude( q ) )

def conjugate( q ):
    """
    Compute the conjugate of the quaternion
    
    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to conjugate
    
    Returns
    -------
    numpy.ndarray (4,)
        The conjugated quaternion
    """
    return np.multiply( q, np.array( [ 1, -1, -1 , -1 ] ) )

def inverse( q ):
    """
    Compute the inverse of the quaternion
    
    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to invert

    Returns
    -------
    numpy.ndarray (4,)
        The inverted quaternion
    """
    return normalize( conjugate( q ) )

def multiply( q1, q2 ):
    """
    Compute the Hamilton product of the two quaternions

    Parameters
    ----------
    q1 : numpy.ndarray (4,)
        The first quaternion
    q2 : numpy.ndarray (4,)
        The second quaternion
    
    Returns
    -------
    numpy.ndarray
        The Hamilton product of the two quaternions [4 elements]
    """
    qm = np.zeros( q1.shape, dtype = np.float )
    qm[0] = q1[0]*q2[0] - q1[1]*q2[1] - q1[2]*q2[2] - q1[3]*q2[3]
    qm[1] = q1[0]*q2[1] + q1[1]*q2[0] + q1[2]*q2[3] - q1[3]*q2[2]
    qm[2] = q1[0]*q2[2] + q1[2]*q2[0] + q1[3]*q2[1] - q1[1]*q2[3]
    qm[3] = q1[0]*q2[3] + q1[3]*q2[0] + q1[1]*q2[2] - q1[2]*q2[1] 
    return qm

def average( q_all, axis = 1 ):
    """
    Average the given set of quaternions

    Parameters
    ----------
    q_all : numpy.ndarray (n_samples, 4 x n_quats) or (4 x n_quats, n_samples)
        The set of quaternions to average
    axis : int
        The axis to average the quaternions over (0 or 1)

    Returns
    -------
    numpy.ndarray (4,)
        The average quaternion

    Notes
    -----
    The formula used here comes from
        Averaging Quaternions
        F. Landis Markley, Yang Cheng, John L. Crassidis and Yaakov Oshman
        http://www.acsu.buffalo.edu/~johnc/ave_quat07.pdf
    """
    n_quats = q_all.shape[axis]
    Q = np.zeros( ( 4, 4 ), dtype = np.float )
    for sample in range(0, n_quats):
        q = q_all[:, sample] if axis == 1 else q_all[sample,:]
        Q = np.outer( q, q ) + Q
    Q = np.divide( Q, n_quats )
    vals, vecs = la.eig( Q )
    return vecs[ :, np.argmax( vals ) ]

def relative( src, dest ):
    """
    Compute the destination quaternion relative to the source
    
    Parameters
    ----------
    src : numpy.ndarray (4,)
        The source quaternion
    dest : numpy.ndarray (4,)
        The destination quaternion
    
    Returns
    -------
    numpy.ndarray
        The quaternion representing the rotation from src to dest
    """
    return normalize( multiply( inverse( src ), dest ) )

def rotate( q, p ):
    """
    Rotate a vector by a quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The rotation quaternion
    p : numpy.ndarray (3,)
        The vector to rotate

    Returns
    -------
    numpy.ndarray
        The rotated vector (3,)
    """
    pp = np.zeros( q.shape, dtype = np.float )
    pp[1:] = p
    pr = multiply( multiply( q, pp ), inverse( q ) )
    return pr[1:]

def between( src, dst ):
    """
    Compute the shortest-arc rotation quaternion from src to dst

    Parameters
    ----------
    src : numpy.ndarray (3,)
        The source vector
    dst : numpy.ndarray (3,)
        The destination vector

    Returns
    -------
    numpy.ndarray
        The shortest-arc rotation quaternion (4,)
    """
    src_norm = np.sqrt( np.sum( np.square( src ) ) )
    dst_norm = np.sqrt( np.sum( np.square( dst ) ) )

    if src_norm < 1e-6 or dst_norm < 1e-6:
        raise RuntimeError( 'Input vector must have non-zero norm!' )

    d = np.dot( src, dst )
    if np.abs( -1.0 - d ) < 1e-6:
        # q is 180 rotation if (src, dst) are anti-parallel
        axis = np.cross( np.array( [1, 0, 0] ), src )
        if np.sqrt( np.sum( np.square( axis ) ) ) < 1e-6: # co-linear
            axis = np.cross( np.array( [ 0, 1, 0 ] ), src )
        q = from_axis_angle( np.pi, axis )
    else:
        w = np.sqrt( src_norm ** 2 * dst_norm ** 2 ) + np.dot( src, dst )
        x, y, z = np.cross( src, dst )
        q = np.array( [ w, x, y, z ] )

    return normalize( q )

def to_euler( q, axes = 'sxyz' ):
    """
    Computes the Euler angle representation from a given quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to compute
    axes : str
        The Euler angle convention (e.g. 'sxyz', 'rxyz', etc.)

    Returns
    -------
    numpy.ndarray (3,)
        The Euler angles

    Notes
    -----
    Valid conventions start with 's' ('static') or 'r' ('rotate') and are followed
    by rotation axes ('xyz', 'zxz', etc.)
    """
    R = to_matrix( q )

    try: firstaxis, parity, repitition, frame = _AXES2TUPLE[axes.lower()]
    except (AttributeError, KeyError):
        _TUPLE2AXES[axes] # validation
        firstaxis, parity, repitition, frame = axes
     
    i = firstaxis
    j = _NEXT_AXIS[i+parity]
    k = _NEXT_AXIS[i-parity+1]

    M = np.array(R, dtype=np.float64, copy=False)[:3, :3]
    if repitition:
        sy = np.sqrt( M[i,j]*M[i,j] + M[i,k]*M[i,k] )
        if sy > _EPS4:
            ax = np.arctan2( M[i,j],  M[i,k] )
            ay = np.arctan2( sy,      M[i,i] )
            az = np.arctan2( M[j,i], -M[k,i] )
        else:
            ax = np.arctan2( -M[j,k], M[j,j] )
            ay = np.arctan2( sy,      M[i,i] )
            az = 0.0
    else:
        cy = np.sqrt( M[i,i]*M[i,i] + M[j,i]*M[j,i] )
        if cy > _EPS4:
            ax = np.arctan2(  M[k,j], M[k,k] )
            ay = np.arctan2( -M[k,i], cy )
            az = np.arctan2(  M[j,i], M[i,i] )
        else:
            ax = np.arctan2( -M[j,k], M[j,j] )
            ay = np.arctan2( -M[k,i], cy )
            az = 0.0

    if parity: ax, ay, az = -ax, -ay, -az
    if frame: ax, az = az, ax

    return np.array( [ax, ay, az] )

def from_euler( angles, axes = 'sxyz' ):
    """
    Computes the quaternion from Euler angle representation

    Parameters
    ----------
    angles : numpy.ndarray (3,)
        The Euler angles
    axes : str
        The Euler angle convention (e.g. 'sxyz', 'rxyz', etc.)

    Returns
    -------
    numpy.ndarray (4,)
        The quaternion to compute

    Notes
    -----
    Valid conventions start with 's' ('static') or 'r' ('rotate') and are followed
    by rotation axes ('xyz', 'zxz', etc.)
    """
    try: firstaxis, parity, repitition, frame = _AXES2TUPLE[axes.lower()]
    except (AttributeError, KeyError):
        _TUPLE2AXES[axes]
        firstaxis, parity, repitition, frame = axes

    i = firstaxis + 1
    j = _NEXT_AXIS[i+parity-1] + 1
    k = _NEXT_AXIS[i-parity] + 1

    ai, aj, ak = angles
    if frame: ai, ak = ak, ai
    if parity: aj = -aj

    ai /= 2.0
    aj /= 2.0
    ak /= 2.0

    ci = np.cos( ai )
    si = np.sin( ai )
    cj = np.cos( aj )
    sj = np.sin( aj )
    ck = np.cos( ak )
    sk = np.sin( ak )

    cc = ci * ck
    cs = ci * sk
    sc = si * ck
    ss = si * sk

    q = np.zeros( 4, dtype = np.float )
    if repitition:
        q[0] = cj * ( cc - ss )
        q[i] = cj * ( cs + sc )
        q[j] = sj * ( cc + ss )
        q[k] = sj * ( cs - sc )
    else:
        q[0] = cj * cc + sj * ss
        q[i] = cj * sc - sj * cs
        q[j] = cj * ss + sj * cc
        q[k] = cj * cs - sj * sc
    if parity: q[j] *= -1.0

    return normalize( q )
    
def to_matrix( q ):
    """
    Compute the rotation matrix from given quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to convert
    
    Returns
    -------
    numpy.ndarray (3, 3)
        The rotation matrix
    """
    q = normalize( q )
    R = np.zeros( (3, 3), dtype = np.float )
    
    R[0,0] = q[0]*q[0] + q[1]*q[1] - q[2]*q[2] - q[3]*q[3]
    R[0,1] = 2 * ( q[1]*q[2] - q[0]*q[3] )
    R[0,2] = 2 * ( q[0]*q[2] + q[1]*q[3] )

    R[1,0] = 2 * ( q[1]*q[2] + q[0]*q[3] )
    R[1,1] = q[0]*q[0] - q[1]*q[1] + q[2]*q[2] - q[3]*q[3]
    R[1,2] = 2 * ( q[2]*q[3] - q[0]*q[1] )

    R[2,0] = 2 * ( q[1]*q[3] - q[0]*q[2] )
    R[2,1] = 2 * ( q[0]*q[1] + q[2]*q[3] )
    R[2,2] = q[0]*q[0] - q[1]*q[1] - q[2]*q[2] + q[3]*q[3]

    return R

def from_matrix( R ):
    """
    Compute the quaternion from the given rotation matrix
    
    Parameters
    ----------
    R : numpy.ndarray (3, 3)
        The rotation matrix
    
    Returns
    -------
    numpy.ndarray (4,)
        The quaternion
    """
    assert( R.shape == (3,3) )
    assert( np.abs( la.det(R) - 1 ) < 1e-6 )
    assert( np.allclose( np.dot( R.T, R ), np.eye(3) ) )
    
    q = np.zeros( 4 , dtype = np.float )
    q[0] = 0.5 * np.sqrt( 1 + R[0,0] + R[1,1] + R[2,2] )
    q[1] = ( 1 / ( 4 * q[0] ) ) * ( R[2,1] - R[1,2] )
    q[2] = ( 1 / ( 4 * q[0] ) ) * ( R[0,2] - R[2,0] )
    q[3] = ( 1 / ( 4 * q[0] ) ) * ( R[1,0] - R[0,1] )

    return normalize( q )

def to_axis_angle( q ):
    """
    Compute the axis-angle representation from a given quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion

    Returns
    -------
    float
        The angle of rotation
    numpy.ndarray (3,)
        The axis of rotation
    """
    q = normalize( q )

    theta = 2.0 * np.arctan2( np.linalg.norm( q[1:] ), q[0] )
    if np.abs( theta ) < 1e-6: axis = np.zeros( 3 )
    else: axis = np.divide( q[1:], np.sin( 0.5 * theta ) )

    return theta, axis

def from_axis_angle( theta, axis ):
    """
    Compute the quaternion from the angle-axis representation

    Parameters
    ----------
    theta : float
        The angle of rotation
    axis : numpy.ndarray (3,)
        The axis of rotation

    Returns
    -------
    numpy.ndarray
        The quaternion (4,)
    """
    q = np.zeros( 4, dtype = np.float )
    q[0] = np.cos( 0.5 * theta )
    q[1:] = np.sin( 0.5 * theta ) * axis
    return normalize( q )

def to_swing_twist( q, axis ):
    """
    Compute the swing-twist quaternion decomposition from a quaternion

    Parameters
    ----------
    q : numpy.ndarray (4,)
        The quaternion to convert
    axis : numpy.ndarray (3,)
        The twist axis

    Returns
    -------
    numpy.ndarray (4,)
        The swing quaternion
    numpy.ndarray (4,)
        The twist quaternion
    """
    axis = axis / np.linalg.norm( axis )
    p = np.dot( q[1:], axis ) * axis
    twist = normalize( np.hstack( [ q[0], p ] ) )
    swing = multiply( q, conjugate( twist ) )
    return swing, twist

def from_swing_twist( swing, twist ):
    """
    Compute the composite quaternion from the swing-twist decomposition

    Parameters
    ----------
    swing : numpy.ndarray (4,)
        The swing quaternion
    twist : numpy.ndarray (4,)
        The twist quaternion

    Returns
    -------
    numpy.ndarray (4,)
        The composite quaternion
    """
    return normalize( multiply( swing, twist ) )

if __name__ == '__main__':
    q = np.array( [ 1 / np.sqrt( 2 ), 1 / np.sqrt( 2 ), 0, 0 ] )
    print( 'Original quaternion: \n', q )

    A = to_euler( q, axes = 'rxyz' )
    print( 'Computed Euler angles: \n', A )

    qA = from_euler( A, axes = 'rxyz' )
    print( 'Recomputed quaternion: \n', qA )

    R = to_matrix( q )
    print( 'Computed rotation matrix: \n', R )

    qR = from_matrix( R )
    print( 'Recomputed quaternion: \n', qR )

    theta, axis = to_axis_angle( q )
    print( 'Computed axis-angle: \n', theta, axis )

    qAA = from_axis_angle( theta, axis )
    print( 'Recomputed quaternion: \n', qAA )

    qswing, qtwist = to_swing_twist( q, axis = np.array( [ 1, 0, 0 ] ) )
    print( 'Computed swing-twist: \n', qswing, qtwist )

    qST = from_swing_twist( qswing, qtwist )
    print( 'Recomputed quaternion: \n', qST )