using System.Xml.Linq;
using UnityEngine;

namespace CreateWindow
{
    internal static class RectExtensions
    {
        public static void SplitVertical(this Rect rect, int y, out Rect top, out Rect bottom)
        {
            top = new Rect(rect);
            bottom = new Rect(rect);

            top.height = y;

            bottom.y += y;
            bottom.height -= y;
        }
        
        public static void SplitHorizontal(this Rect rect, int x, out Rect left, out Rect right)
        {
            left = new Rect(rect);
            right = new Rect(rect);

            left.width = x;

            right.x += x;
            right.width -= x;
        }

        public static Rect Padding(this Rect rect, int padding)
        {
            return  new Rect(rect.x + padding, rect.y + padding, rect.width - 2 * padding, rect.height - 2 * padding);
        }
    }
}