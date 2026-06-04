using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the 13-cell center gem pile. Filled cells switch to empty as gems are claimed.
    /// Stage 5 Branch 4 adds cell position queries for the gem flight animator.
    /// </summary>
    public class CoinGridView : MonoBehaviour
    {
        [SerializeField] private RectTransform cellContainer;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private int totalCells = 13;

        // 생성된 셀 이미지 참조 (인덱스 0이 가장 먼저 비워질 셀)
        private readonly List<Image> _cells = new List<Image>();

        // 현재 채워진 셀 수. SetRemaining이 갱신.
        private int _currentRemaining;

        /// <summary>Total number of cells configured for this grid (typically 13).</summary>
        public int TotalCells => totalCells;

        /// <summary>Current number of filled cells. Reflects the last SetRemaining call.</summary>
        public int CurrentRemaining => _currentRemaining;

        /// <summary>
        /// Spawn totalCells cells, all initially filled.
        /// Should be called once when the match starts.
        /// </summary>
        public void Initialize()
        {
            // 기존에 생성된 셀이 있다면 모두 제거 (씬 재로드 대응)
            foreach (Image cell in _cells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }
            _cells.Clear();

            // totalCells 만큼 새 셀 생성
            for (int i = 0; i < totalCells; i++)
            {
                GameObject go = Instantiate(cellPrefab, cellContainer);
                Image img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.color = UIColors.GemFilled;
                    _cells.Add(img);
                }
            }

            _currentRemaining = totalCells;
        }

        /// <summary>
        /// Update which cells are filled. The first `remaining` cells stay filled; the rest go empty.
        /// </summary>
        public void SetRemaining(int remaining)
        {
            // 음수/초과 값 보정
            remaining = Mathf.Clamp(remaining, 0, _cells.Count);
            _currentRemaining = remaining;

            for (int i = 0; i < _cells.Count; i++)
            {
                // 인덱스가 remaining 미만이면 채워짐, 그 외는 비어있음
                _cells[i].color = (i < remaining) ? UIColors.GemFilled : UIColors.GemEmpty;
            }
        }

        /// <summary>
        /// Returns the world-space positions of the rightmost `count` currently-filled cells.
        /// Used by the gem flight animator as starting points when a player claims gems.
        /// The order is rightmost-first so that the cells visually "consumed" match the
        /// cells that will be emptied next.
        /// </summary>
        public List<Vector3> GetRightmostFilledCellPositions(int count)
        {
            List<Vector3> result = new List<Vector3>();

            // 채워진 셀 범위는 [0, _currentRemaining). 그 중 오른쪽 끝부터 count개.
            // 예: _currentRemaining=10, count=3 이면 인덱스 9, 8, 7 순서.
            int taken = 0;
            for (int i = _currentRemaining - 1; i >= 0 && taken < count; i--)
            {
                Image cell = _cells[i];
                if (cell == null) continue;

                result.Add(cell.transform.position);
                taken++;
            }

            return result;
        }
    }
}
