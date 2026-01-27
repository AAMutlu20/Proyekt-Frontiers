using UnityEngine;

namespace Audio.Bridges
{
    /// <summary>
    /// Handles audio for tower shooting and other actions
    /// Add this component to your tower prefabs
    /// </summary>
    public class TowerAudioController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioLibrary audioLibrary;
        [SerializeField] private bool playShootSoundOnFire = true;
        
        /// <summary>
        /// Call this when the tower shoots
        /// </summary>
        public void OnShoot()
        {
            if (playShootSoundOnFire && audioLibrary && audioLibrary.towerShoot)
            {
                AudioManager.Instance?.PlaySound3D(audioLibrary.towerShoot, transform.position);
            }
        }
        
        /// <summary>
        /// Call this when tower is upgraded
        /// </summary>
        public void OnUpgrade()
        {
            if (audioLibrary && audioLibrary.towerUpgrade)
            {
                AudioManager.Instance?.PlaySound3D(audioLibrary.towerUpgrade, transform.position);
            }
        }
    }
}