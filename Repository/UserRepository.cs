using ForecastingModule.Model;
using ForecastingModule.Util;

namespace ForecastingModule.Repository
{
    internal interface UserRepository
    {
        Optional<UserDto> findUserByName(string userName, bool active = true);
    }
}
