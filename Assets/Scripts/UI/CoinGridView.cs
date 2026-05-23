using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the 13-cell center gem pile. Filled cells switch to empty as gems are claimed.
    /// </summary>
    public class CoinGridView : MonoBehaviour
    {
        [SerializeField] private RectTransform cellContainer;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private int totalCells = 13;

        // 생성된 셀 이미지 참조 (인덱스 0이 가장 먼저 비워질 셀)
        private readonly List<Image> _cells = new List<Image>();

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
        }

        /// <summary>
        /// Update which cells are filled. The first `remaining` cells stay filled; the rest go empty.
        /// </summary>
        public void SetRemaining(int remaining)
        {
            // 음수/초과 값 보정
            remaining = Mathf.Clamp(remaining, 0, _cells.Count);

            for (int i = 0; i < _cells.Count; i++)
            {
                // 인덱스가 remaining 미만이면 채워짐, 그 외는 비어있음
                _cells[i].color = (i < remaining) ? UIColors.GemFilled : UIColors.GemEmpty;
            }
        }
    }
}