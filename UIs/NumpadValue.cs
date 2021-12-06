namespace StereoKitApp.UIs
{
    internal enum NumpadValue
    {
        Err = -1,
        Num0 = 0,
        // ReSharper disable UnusedMember.Global -- Created dynamically
        Num1 = 1,
        Num2 = 2,
        Num3 = 3,
        Num4 = 4,
        Num5 = 5,
        Num6 = 6,
        Num7 = 7,
        Num8 = 8,
        Num9 = 9,
        // ReSharper restore UnusedMember.Global
        Dot = 100,
        Submit = 101
    }

    internal static class NumpadValueExtensions
    {
        /// <summary>
        /// Returns the actual number as an int if it was not a button or similar that was clicked.
        /// </summary>
        /// <param name="numpadValue"></param>
        /// <param name="number"></param>
        /// <returns>True if its a number</returns>
        public static bool TryGetNumber(this NumpadValue numpadValue, out int number)
        {
            var numpadValueAsInt = (int)numpadValue;
            if (numpadValueAsInt >= 0 && numpadValueAsInt <= 9)
            {
                number = numpadValueAsInt;
                return true;
            }

            number = -1;
            return false;
        }
    }
}
