using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SocketInterface
{
    class ConsoleClient
    {
        public static Session session;
        public static bool IsConnected { get { return session == null ? false : session.Connected; } }

        public static async Task HandleClient(string endpointURL)
        {
            try
            {
                Console.WriteLine("1 - Create an Application Configuration.");
                Utils.SetTraceOutput(Utils.TraceOutput.DebugAndFile);
                var config = new ApplicationConfiguration()
                {
                    ApplicationName = "Console Client",
                    ApplicationType = ApplicationType.Client,
                    ApplicationUri = "urn:" + Utils.GetHostName() + ":Beenen:ConsoleClient",
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "X509Store",
                            StorePath = "CurrentUser\\UA_MachineDefault",
                            SubjectName = "Console Client"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/UA Applications",
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities",
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/RejectedCertificates",
                        },
                        NonceLength = 32,
                        AutoAcceptUntrustedCertificates = true
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
                };

                await config.Validate(ApplicationType.Client);

                bool haveAppCertificate = config.SecurityConfiguration.ApplicationCertificate.Certificate != null;

                if (!haveAppCertificate)
                {
                    Console.WriteLine("    INFO: Creating new application certificate: {0}", config.ApplicationName);

                    X509Certificate2 certificate = CertificateFactory.CreateCertificate(
                        config.SecurityConfiguration.ApplicationCertificate.StoreType,
                        config.SecurityConfiguration.ApplicationCertificate.StorePath,
                        null,
                        config.ApplicationUri,
                        config.ApplicationName,
                        config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                        null,
                        CertificateFactory.defaultKeySize,
                        DateTime.UtcNow - TimeSpan.FromDays(1),
                        CertificateFactory.defaultLifeTime,
                        CertificateFactory.defaultHashSize,
                        false,
                        null,
                        null
                        );

                    config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;

                }

                haveAppCertificate = config.SecurityConfiguration.ApplicationCertificate.Certificate != null;

                if (haveAppCertificate)
                {
                    config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);

                    if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                    {
                        config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                    }
                }
                else
                {
                    Console.WriteLine("    WARN: missing application certificate, using unsecure connection.");
                }

                Console.WriteLine("2 - Discover endpoints of {0}.", endpointURL);
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, haveAppCertificate, 15000);
                Console.WriteLine("    Selected endpoint uses: {0}",
                    selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1));

                Console.WriteLine("3 - Create a session with OPC UA server.");
                var endpointConfiguration = EndpointConfiguration.Create(config);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
                session = await Session.Create(config, endpoint, false, config.ApplicationName, 60000, new UserIdentity(new AnonymousIdentityToken()), null);

                Console.WriteLine("4 - Browse the OPC UA server namespace.");
                ReferenceDescriptionCollection references;
                Byte[] continuationPoint;

                references = session.FetchReferences(ObjectIds.ObjectsFolder);

                session.Browse(
                    null,
                    null,
                    ObjectIds.ObjectsFolder,
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                    out continuationPoint,
                    out references);

                Console.WriteLine(" DisplayName, BrowseName, NodeClass");
                foreach (var rd in references)
                {
                    Console.WriteLine(" {0}, {1}, {2}", rd.DisplayName, rd.BrowseName, rd.NodeClass);
                    ReferenceDescriptionCollection nextRefs;
                    byte[] nextCp;
                    session.Browse(
                        null,
                        null,
                        ExpandedNodeId.ToNodeId(rd.NodeId, session.NamespaceUris),
                        0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences,
                        true,
                        (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                        out nextCp,
                        out nextRefs);

                    foreach (var nextRd in nextRefs)
                    {
                        Console.WriteLine("   + {0}, {1}, {2}", nextRd.DisplayName, nextRd.BrowseName, nextRd.NodeClass);
                    }
                }

                Console.WriteLine("5 - Create a subscription with publishing interval of 1 second.");
                var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000 };

                Console.WriteLine("6 - Add a list of items (server current time and status) to the subscription.");
                var list = new List<MonitoredItem> {
                new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = "ServerStatusCurrentTime", StartNodeId = "i=2258"
                }
            };
                list.ForEach(i => i.Notification += OnNotification);
                subscription.AddItems(list);

                Console.WriteLine("7 - Add the subscription to the session.");
                session.AddSubscription(subscription);
                subscription.Create();

                Console.WriteLine("8 - Running...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OnNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            foreach(var value in monitoredItem.DequeueValues())
            {
                //Console.WriteLine("{0}: {1}, {2}, {3}", monitoredItem.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }
    }
}
