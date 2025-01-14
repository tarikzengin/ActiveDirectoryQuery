/****************************** Module Header ******************************
 * Module Name:  ActiveDirectoryQuery console application
 * Project:      ActiveDirectoryQuery console application to query all users in the AD, and possibly
 *               find all data for a certain user (furnished from the command line)
 * (C) None -    Use as needed!
 *
 * Program : Main program to demonstrate querying Active Directory
 *
 * Revisions:
 *     1. Sundar Krishnamurthy         sundar_k@hotmail.com               2/8/2016       Initial file created.
***************************************************************************/

namespace ActiveDirectoryQuery {

    #region Using directives
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    #endregion

    #region Program class
    /// <summary>
    /// Program class - Main program to demonstrate querying Active Directory
    /// </summary>
    internal class Program {

        #region Methods
        #region Public/Internal Methods
        /// <summary>
        /// Main method - entry point of application
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        internal static void Main(string[] args) {

            var domain = ConfigurationManager.AppSettings["CorporateDomain"];

            // List all accounts in the Active Directory
            Program.ListAllAccounts(domain);

            // If command line argument is provided, query and find all details for this user
            if (args.Length > 0) {
                Program.QueryUser(args[0], domain);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// List all accounts in the Active Directory
        /// </summary>
        /// <param name="domain">Domain</param>
        private static void ListAllAccounts(string domain) {

            try {

                // Construct context to query your Active Directory
                using (var context = new PrincipalContext(ContextType.Domain, domain)) {

                    // Construct UserPrincipal object for this context
                    var userPrincipal = new UserPrincipal(context);

                    // Search and find every user in the system – PrincipalSearcher instance for what we need!
                    using (var searcher = new PrincipalSearcher(userPrincipal)) {

                        var counter = 0u;

                        // Iterate for all users in AD
                        foreach (var result in searcher.FindAll()) {

                            counter++;
                            var de = result.GetUnderlyingObject() as DirectoryEntry;
                            var samAccountName = de.Properties["samAccountName"].Value;
                            var active = IsUserActiveInAD(de);
                            Console.WriteLine("{0}: {1} - {2}", counter, samAccountName, active ? "Yes" : "No");
                        }
                    }
                }
            } catch (PrincipalServerDownException ex) {
                Console.WriteLine(string.Format("Unable to lookup domain: {0}\r\n{1}", domain, ex.ToString()));
            }
        }

        /// <summary>
        /// Query User in Active Directory
        /// </summary>
        /// <param name="samAccountName">samAccountName</param>
        /// <param name="domain">Domain</param>
        private static void QueryUser(string samAccountName, string domain) {

            // Construct context to query your Active Directory
            using (var context = new PrincipalContext(ContextType.Domain, domain)) {

                // Construct UserPrincipal object
                var userPrincipal = new UserPrincipal(context);
                userPrincipal.SamAccountName = samAccountName;

                // Search and find every user in the system – PrincipalSearcher for our purpose!
                using (var searcher = new PrincipalSearcher(userPrincipal)) {

                    var timeNames = new string[] { "whenCreated", "whenChanged", "lastLogonTimestamp", "lastLogon", "lastLogoff", "accountExpires" };

                    var excludeNames = new List<string>();

                    foreach (var timeName in timeNames) {
                        excludeNames.Add(timeName);
                    }

                    excludeNames.Add("memberOf");

                    var result = searcher.FindOne();

                    if (result != null) {

                        var de = result.GetUnderlyingObject() as DirectoryEntry;

                        foreach (var property in de.Properties) {

                            var prop = property as System.DirectoryServices.PropertyValueCollection;

                            if (!excludeNames.Contains(prop.PropertyName)) {
                                Console.WriteLine(string.Format("{0} = {1}", prop.PropertyName, de.Properties[prop.PropertyName].Value));
                            }
                        }

                        Console.WriteLine(string.Format("Active: {0}", IsUserActiveInAD(de)));
                        Console.WriteLine(string.Empty);

                        DateTime time = DateTime.MinValue;

                        var count = 0u;

                        foreach (var value in timeNames) {

                            if (de.Properties.Contains(value)) {
                                try {
                                    count++;

                                    if (count < 3u) {
                                        time = (DateTime)de.Properties[value].Value;
                                    } else {
                                        var li = ((ActiveDs.IADsLargeInteger)(de.Properties[value].Value));
                                        long date = (long)li.HighPart << 32 | (uint)li.LowPart;
                                        time = DateTime.FromFileTime(date);
                                    }
                                } catch (Exception ex) {
                                    if (count != 5u) {
                                        Console.WriteLine(ex.ToString());
                                        throw;
                                    }
                                }
                            }

                            if (time != DateTime.MinValue) {
                                Console.WriteLine(string.Format("{0} = {1}", value, time.ToString()));
                            } else {
                                Console.WriteLine(string.Format("{0} not set", value));
                            }
                        }

                        Console.WriteLine("Groups:");
                        var groups = GetGroups(de);

                        foreach (var group in groups) {
                            Console.WriteLine(string.Format("\t{0}", group));
                        }
                    } else {
                        Console.WriteLine(string.Format("No data found for samAccountName: {0}", samAccountName));
                    }
                }
            }
        }

        /// <summary>
        /// Is this user Active in AD?
        /// </summary>
        /// <param name="de">DirectoryEntry instance retrieved from AD</param>
        /// <returns>True if this user is active, false otherwise</returns>
        private static bool IsUserActiveInAD(DirectoryEntry result) {

            bool active = false;

            if (result.NativeGuid == null) return false;

           if (result.Properties.Contains("userAccountControl")) {
                int flags = (int)result.Properties["userAccountControl"].Value;

                active = !Convert.ToBoolean(flags & 0x0002);

                if (active) {
                   if ((result.Path.Contains("OU=Archived,OU=Terms")) ||
                       (result.Path.Contains("OU=Archived")) ||
                       (result.Path.Contains("OU=Terms"))) {
                       active = false;
                    }
                }
            }

            return active;
        }

        /// <summary>
        /// Get Groups that this user is a member of
        /// </summary>
        /// <param name="result">DirectoryEntry instance retrieved from AD</param>
        /// <returns>List of group names</returns>
        private static List<string> GetGroups(DirectoryEntry result) {

            var groupNames = new List<string>();

            int propertyCount = result.Properties["memberOf"].Count;

            for (int i = 0; i < propertyCount; i++) {
                var dn = result.Properties["memberOf"][i] as string;

                var equalsIndex = dn.IndexOf("=", 1);
                var commaIndex = dn.IndexOf(",", 1);
                if (-1 == equalsIndex) {
                    break;
                }

                groupNames.Add(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
            }

            return groupNames;
        }
        #endregion
        #endregion
    }
    #endregion
}
