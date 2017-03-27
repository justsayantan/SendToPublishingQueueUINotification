using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Extensibility;
using Tridion.ContentManager.Extensibility.Events;
using Tridion.ContentManager.Notifications;
using Tridion.ContentManager.Publishing;
using Tridion.Logging;

namespace SentToPublishNotification.EventHandler
{
    [TcmExtension("SentToPublishNotification")]
    public class SentToPublishEventHandler : TcmExtension
    {
        public SentToPublishEventHandler()
        {
            MySubscribe();
        }

        public void MySubscribe()
        {
            //here I have define the signature of the Subcribe method
            //Subscribe<TSubject, TEvent>(TcmEventHandler<TSubject, TEvent> eventHandler, EventPhases phases, EventSubscriptionOrder order)
            //This signature is same for both Subscribe and SubscribeAsync method.

            EventSystem.Subscribe<Page, SaveEventArgs>(OnPageSave, EventPhases.Processed);
        }

        private void OnPageSave(Page subject, SaveEventArgs e, EventPhases phase)
        {
            try
            {
                #region Event System Code
                
                Session session = subject.Session;
                Page page = subject;

                if (page.IsCheckedOut)
                {
                    page.CheckIn();
                }


                IEnumerable<IdentifiableObject> items = new List<IdentifiableObject> { page };

                /*in the previous version we have to define the publishing target before calling the publish method. 
                 * Now in web 8 there is new concept introduced “Purpose” to manage all the publishing target settings 
                 * directly into the Topology Manager. So In web 8 instead of defining the target you can also define 
                 * the Purpose which I think more user-friendly and meaningful for a developer.*/

                string[] purpose = new[] { "Staging" };

                //Now you have to define the Publish Instruction detail
                PublishInstruction instruction = new PublishInstruction(session)
                {
                    DeployAt = DateTime.Now,
                    MaximumNumberOfRenderFailures = 10,
                    RenderInstruction = new RenderInstruction(session),
                    ResolveInstruction = new ResolveInstruction(session),
                    StartAt = DateTime.MinValue
                };
                
                //PublishEngine.Publish(IEnumerable<IdentifiableObject>  items,PublishInstruction publishInstruction,IEnumerable<string> purpose, PublishPriority publishPriority);
                PublishEngine.Publish(items, instruction, purpose, PublishPriority.High);

                #endregion

                #region Send Notification

                // create an object that contains some data that we want to send to the client as the details of the message
                JObject details = JObject.FromObject(new
                {
                    creatorId = subject.Creator.Id.ToString(),
                    purpose = "Staging",
                    title = subject.Title
                });

                NotificationMessage message = new NotificationMessage
                {
                    // we need an identifier that we can use in the UI extension to distinguish our messages from others
                    Action = "tcm:senttopublishfinished",
                    SubjectIds = new[] { subject.Id.ToString() },
                    Details = details.ToString()
                };

                subject.Session.NotificationsManager.BroadcastNotification(message);

                #endregion

                if (!page.IsCheckedOut)
                {
                    page.CheckOut();
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Exception: {ex.Message}", "SentToPublishEventHandler", LoggingCategory.General, System.Diagnostics.TraceEventType.Information);
            }
        }
    }
}
