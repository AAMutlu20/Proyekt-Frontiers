using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace irminNavmeshEnemyAiUnityPackage
{
    public class HealthUI : MonoBehaviour
    {
        private int _maxHearts;
        private int _currentHearts;
        private int _currentExtraHearts;
        private bool _wasFinalHit = false;
        [SerializeField] private List<RawImage> _spawnedHearts = new();

        public float Health { get { return _currentHearts; } set { SetCurrentHearts(Mathf.RoundToInt(value)); } }
        public bool WasFinalHit { get { return _wasFinalHit; } set { _wasFinalHit = value; } }

        [SerializeField] private RawImage _heartImagePrefab;
        [SerializeField] private Transform _heartImageSpawnTransform;

        [SerializeField] Color _heartColor;
        [SerializeField] Color _extraHeartColor;
        [SerializeField] Color _lostHeartColor;

        [SerializeField] bool _debug = false;

        private void Start()
        {
            // Debug Test
            if (_debug)
            {
                StartCoroutine(Test());
            }

        }

        //public void DoDamage(float pDamage)
        //{
        //    if (_currentHearts - pDamage <= 0)
        //    {
        //        _wasFinalHit = true;
        //        _currentHearts = 0;
        //        SetCurrentHearts();
        //    }
        //    else
        //    {
        //        _currentHearts -= Mathf.RoundToInt(pDamage);
        //        SetCurrentHearts();
        //    }
        //}

        public void SetMaxHearts(int pHearts)
        {
            _maxHearts = pHearts;
            if (_spawnedHearts.Count < _maxHearts)
            {
                AddTillCorrectHearts();
            }
            else if (_spawnedHearts.Count > _maxHearts)
            {
                RemoveTillCorrectHearts();
            }
        }

        public void SetMaxHearts()
        {
            SetMaxHearts(_maxHearts);
        }

        public void SetCurrentHearts(int pHearths, int pExtraHearths = -1)
        {
            _currentHearts = pHearths;
            if (pExtraHearths >= 0) _currentExtraHearts = pExtraHearths;
            UpdateHeartColor();
        }

        public void SetCurrentHearts()
        {
            SetCurrentHearts(_currentHearts);
        }

        public void UpdateHeartColor()
        {
            Debug.Log($"Updating Heart Color: Current Hearts{_currentHearts} Current Extra Hearts{_currentExtraHearts}");
            for (int i = 0; i < _spawnedHearts.Count; i++)
            {
                if (i > _currentHearts - 1)
                {
                    if (i > (_currentHearts - 1 + _currentExtraHearts))
                    {
                        _spawnedHearts[i].color = _lostHeartColor;
                    }
                    else if (_currentExtraHearts > 0)
                    {
                        _spawnedHearts[i].color = _extraHeartColor;
                    }

                }
                else
                {
                    _spawnedHearts[i].color = _heartColor;
                }
            }
        }

        private void RemoveTillCorrectHearts()
        {
            while (_spawnedHearts.Count > _maxHearts)
            {
                Destroy(_spawnedHearts[_spawnedHearts.Count - 1].gameObject);
                _spawnedHearts.RemoveAt(_spawnedHearts.Count - 1);
            }
        }

        private void AddTillCorrectHearts()
        {
            while (_spawnedHearts.Count < _maxHearts)
            {
                RawImage spawnedHeart = Instantiate(_heartImagePrefab.gameObject, _heartImageSpawnTransform).GetComponent<RawImage>();
                _spawnedHearts.Add(spawnedHeart);
            }
        }

        private IEnumerator Test()
        {
            yield return new WaitForSeconds(5);
            SetMaxHearts(10);
            SetCurrentHearts(5);
            yield return new WaitForSeconds(5);
            SetMaxHearts(5);
            SetCurrentHearts(3, 1);
            yield return new WaitForSeconds(5);
            SetMaxHearts(2);
            SetCurrentHearts(1);
            yield return new WaitForSeconds(5);
            SetMaxHearts(10);
            SetCurrentHearts(6, 2);
        }
    }
}