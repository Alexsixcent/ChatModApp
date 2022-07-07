using System.Reflection;

namespace ChatModApp.Shared.Tools.Extensions;

public static class ObjectExtensions
{
    private const BindingFlags StaticFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    //From: https://stackoverflow.com/a/51775281
    //Used to avoid reimplementing IRC parsing from TwitchLib or including their code
    public static void RaiseEvent(this object instance, string eventName, EventArgs e)
    {
        var type = instance.GetType();
        var eventField = type.GetField(eventName, StaticFlags);
        if (eventField == null)
            throw new InvalidOperationException($"Event with name {eventName} could not be found.");
        var multicastDelegate = eventField.GetValue(instance) as MulticastDelegate;
        if (multicastDelegate == null)
            return;

        var invocationList = multicastDelegate.GetInvocationList();

        foreach (var invocationMethod in invocationList)
            invocationMethod.DynamicInvoke(instance, e);
    }
}