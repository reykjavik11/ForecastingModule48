using System;
using ForecastingModule.Repository.Impl;

namespace ForecastingModule.Service.Impl
{
    internal sealed class UserServiceImpl : UserService
    {
        private static readonly Lazy<UserServiceImpl> _instance = new Lazy<UserServiceImpl>(() => new UserServiceImpl());
        private readonly UserRepositoryImpl repository = UserRepositoryImpl.Instance;
        public static UserServiceImpl Instance => _instance.Value;

        private UserServiceImpl()
        {
        }
        public bool findUserByName(string userName)
        {
            Util.Optional<Model.UserDto> optionalUser = repository.findUserByName(userName);
            setSessionUser(optionalUser);
            return optionalUser.HasValue;
        }

        private static void setSessionUser(Util.Optional<Model.UserDto> optionalUser)
        {
            if (optionalUser.HasValue)
            {
                UserSession.Initialize(optionalUser.Get());
            }
        }
    }
}
