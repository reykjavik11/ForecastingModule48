using ForecastingModule.Model;
using ForecastingModule.Helper;

namespace ForecastingModule.Repository
{
    internal interface UserRepository
    {
        Optional<UserDto> findUserByName(string userName, bool active = true);
    }
}
