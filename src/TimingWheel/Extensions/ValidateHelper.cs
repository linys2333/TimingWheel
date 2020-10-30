using System;

namespace TimingWheel.Extensions
{
    /// <summary>
    /// 入参校验
    /// </summary>
    public static class Requires
    {
        public static void NotNull(object obj, string name)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotNullOrEmpty(string str, string name)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
