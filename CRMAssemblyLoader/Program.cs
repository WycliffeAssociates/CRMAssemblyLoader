using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Query;

namespace CRMAssemblyLoader
{
    class Program
    {
        static private Dictionary<string, Type> Plugins = new Dictionary<string, Type>();
        static private string connectionString;
        static private string path;
        static private CrmServiceClient service;
        static private Assembly a;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments");
                Environment.Exit(1);
            }
            connectionString = args[0];
            path = args[1];
            if (File.Exists(path))
            {
                LoadAssemblyInformation();

                var file = File.ReadAllBytes(path);
                service = new CrmServiceClient(connectionString);
                if (!string.IsNullOrEmpty(service.LastCrmError))
                {
                    Console.WriteLine("Error connecting");
                    Console.WriteLine(service.LastCrmError);
                    Environment.Exit(1);
                }


                QueryExpression query = new QueryExpression("pluginassembly");
                query.ColumnSet = new ColumnSet("content");
                query.Criteria.AddCondition("name", ConditionOperator.Equal, a.GetName().Name);
                var result = service.RetrieveMultiple(query);
                Entity pluginAssembly = result.Entities[0];

                List<Entity> currentPlugins = ListPluginSteps(pluginAssembly.Id);
                Dictionary<string, Guid> registeredPluginSteps = new Dictionary<string, Guid>();
                Console.WriteLine("Checking for missing plugins in source assembly");
                foreach (Entity e in currentPlugins)
                {
                    string fullName = (string)e["typename"];
                    registeredPluginSteps.Add(fullName, e.Id);
                    bool shouldStop = false;
                    if (!Plugins.ContainsKey(fullName))
                    {
                        Console.WriteLine($"{fullName} not found in assembly");
                        shouldStop = true;
                    }

                    if (shouldStop)
                    {
                        Environment.Exit(1);
                    }
                }
                Console.WriteLine("Updating assembly");
                pluginAssembly["content"] = Convert.ToBase64String(file);
                service.Update(pluginAssembly);

                Console.WriteLine("Adding unregistered plugins");
                foreach (var item in Plugins)
                {
                    if (!registeredPluginSteps.ContainsKey(item.Key))
                    {
                        Entity pluginStep = new Entity("plugintype");
                        pluginStep["typename"] = item.Value.FullName;
                        pluginStep["name"] = item.Value.FullName;
                        pluginStep["friendlyname"] = Guid.NewGuid().ToString();
                        pluginStep["pluginassemblyid"] = pluginAssembly.ToEntityReference();

                        if (item.Value.BaseType == typeof(CodeActivity))
                        {
                            pluginStep["workflowactivitygroupname"] = $"{a.GetName().Name} ({a.GetName().Version.ToString()})";
                            pluginStep["name"] = item.Value.FullName;
                        }

                        service.Create(pluginStep);
                    }
                    else
                    {
                        //If it is a workflow step we need to update it so that argument changes are visible in the workflow editor
                        if (item.Value.BaseType == typeof(CodeActivity))
                        {
                            Entity currentStep = service.Retrieve("plugintype", registeredPluginSteps[item.Key], new ColumnSet("typename", "friendlyname", "workflowactivitygroupname"));
                            service.Update(currentStep);
                        }
                    }
                }


            }
            else
            {
                Console.WriteLine("Can't load assembly");
                Environment.Exit(2);
            }
            Console.WriteLine("All done");
        }
        private static List<Entity> ListPluginSteps(Guid assemblyId)
        {
            QueryExpression query = new QueryExpression("plugintype");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);

            EntityCollection result = service.RetrieveMultiple(query);
            return result.Entities.ToList();
        }
        private static void LoadAssemblyInformation()
        {
            Plugins = new Dictionary<string, Type>();
            a = Assembly.LoadFrom(path);
            foreach (Type t in a.ExportedTypes)
            {
                if (t.BaseType == typeof(CodeActivity) || t.GetInterface("IPlugin") != null)
                {
                    Plugins.Add(t.FullName, t);
                }
            }
        }
    }

}
