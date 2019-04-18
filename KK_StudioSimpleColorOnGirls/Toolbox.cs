using System.Reflection;

namespace Extension
{
    public static class Extension
    {
        public static object GetPrivateProperty(this object self, string name)
        {
            PropertyInfo propertyInfo;
            propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            return propertyInfo.GetValue(self, null);
        }
    }
}