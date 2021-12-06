namespace StereoKitApp.Utils
{
    /// <summary>
    /// Hack to store references in lists etc.
    /// Construct this by sending in a ref value, and get the reference by using <see cref="GetValueRef"/>
    /// </summary>
    /// <typeparam name="T">Any struct reference</typeparam>
    public class RefWrapper<T> where T : struct
    {
        private T _value;

        /// <summary>
        /// Get a reference to this RefValue.
        /// Remember to store it in a `ref var` if you want it mutable :)
        /// </summary>
        /// <returns>A reference to this Value</returns>
        public ref T GetValueRef()
        {
            return ref _value;
        }

        internal RefWrapper(T value)
        {
            _value = value;
        }
    }
}
