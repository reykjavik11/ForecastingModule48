using System;
using ForecastingModule.Model;

namespace ForecastingModule.Service
{
    internal sealed class UserSession
    {
        private static UserSession instance = null;

        private static readonly object lockObject = new object();

        public UserDto User { get; private set; }

        private UserSession(UserDto userDto)
        {
            User = userDto;
        }

        public static UserSession Initialize(UserDto userDto)
        {
            if (instance == null)
            {
                lock (lockObject) // Ensure thread safety
                {
                    if (instance == null)
                    {
                        instance = new UserSession(userDto);
                    }
                }
            }

            return instance;
        }

        public static UserSession GetInstance()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("UserSession has not been initialized. Call Initialize first.");
            }

            return instance;
        }

    }

}
