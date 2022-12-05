using Microsoft.AspNetCore.Authentication;

namespace rsproxy
{
    public class ArgParser
    {
        public static T Parse<T>(string[] args) where T: new()
        {
            T parsed = new T() { };
            for(int i=0; i<args.Length; ++i)
            {
                var arg = args[i];
                if (arg.StartsWith("/") || arg.StartsWith("-"))
                {
                    // Find the property
                    var name = arg.Substring(1);
                    var prop = typeof(T).GetProperties().SingleOrDefault(p => p.Name == name);
                    if (prop == null)
                        continue;
                    if (prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(parsed, true);
                    }
                    else if(i+1 < args.Length)
                    {
                        prop.SetValue(parsed, args[++i]);
                    }
                }
            }
            return parsed;
        }
    }
}
