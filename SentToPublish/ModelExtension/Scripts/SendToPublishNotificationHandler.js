var notificationHandler = function(event)
{
    // Only proceed if the message is the message we broadcasted ourself from the CM eventhandler
    if (event.data.action !== "tcm:senttopublishfinished")
    {
        return;
    }
    var userSettings = Tridion.ContentManager.UserSettings.getInstance();

    //Restrict the message for particular user.
    // Only process the message when it is meant for this user.
    if (event.data.details.creatorId === userSettings.getUserId())
    {
        if (Tridion.MessageCenter.getInstance())
        {

            var title = "Sent Item For Publishing";
            var description = `Your item '${event.data.details.title}' has been sent for publishing to '${event.data.details.purpose}'`;

            Tridion.MessageCenter.registerNotification(title, description, true);
        }
    }
};
var notificationBroadcaster = Tridion.Web.UI.Core.NotificationBroadcaster.getInstance();
notificationBroadcaster.addEventListener("notification", notificationHandler);
