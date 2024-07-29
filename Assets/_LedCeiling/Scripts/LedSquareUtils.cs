using UnityEngine;

public static class LedSquareUtils
{
    // each square is 62 x 62, with a 1px gap in the corner

    // index is 0-239
    public static Vector2Int LedIndexToXY(int i, Vector2Int position, bool counterClockwise)
    {
        int side = i / 60;
        int indexOnSide = i % 60;

        int x = 0;
        int y = 0;

        if (counterClockwise)
        {
            // Counter clockwise
            switch (side)
            {
                case 0:
                    x = 60 - indexOnSide;
                    y = 61;
                    break;

                case 1:
                    x = 0;
                    y = indexOnSide + 1;
                    break;

                case 2:
                    x = indexOnSide + 1;
                    y = 0;
                    break;

                case 3:
                    x = 61;
                    y = 60 - indexOnSide;
                    break;
            }
        }
        else
        {
            // Clockwise
            switch (side)
            {
                case 0:
                    x = 0;
                    y = indexOnSide + 1;
                    break;

                case 1:
                    x = indexOnSide + 1;
                    y = 61;
                    break;

                case 2:
                    x = 61;
                    y = 60 - indexOnSide;
                    break;

                case 3:
                    x = 60 - indexOnSide;
                    y = 0;
                    break;
            }
        }
        

        return new Vector2Int(x, y) + position;
    }
}