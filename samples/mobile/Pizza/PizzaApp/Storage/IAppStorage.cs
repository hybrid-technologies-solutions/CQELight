using System.Threading.Tasks;

namespace PizzaApp.Storage
{
    public interface IAppStorage
    {
        Task StoreValueAsync(string key, object value);
        T GetValue<T>(string key);
    }
}