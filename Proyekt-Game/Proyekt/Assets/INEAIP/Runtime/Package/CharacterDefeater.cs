// Defeats characters instantly, mostly made to test anti killbox manouvres.
using UnityEngine;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class CharacterDefeater : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"CharacterDefeater hit {other.name}");
            BaseCharacter character = other.gameObject.GetComponent<BaseCharacter>();
            if (character != null)
            {
                Debug.Log($"CharacterDefeater hit {other.name} which was detected as a character");
                character.Defeat();
            }
        }
    }
}