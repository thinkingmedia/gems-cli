﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GemsCLI.Arguments;
using GemsCLI.Attributes;
using GemsCLI.Descriptions;
using GemsCLI.Enums;
using GemsCLI.Help;
using GemsCLI.Output;
using GemsCLI.Validators;

namespace GemsCLI
{
    public static class RequestFactory
    {
        /// <summary>
        /// Finds an attribute for a property.
        /// </summary>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <param name="pInfo">The property</param>
        /// <returns>An attribute or null.</returns>
        private static T Attribute<T>(PropertyInfo pInfo) where T : Attribute
        {
            return (T)pInfo.GetCustomAttributes(true).FirstOrDefault(pAttr=>pAttr is T);
        }

        /// <summary>
        /// </summary>
        /// <param name="pOptions"></param>
        /// <param name="pArgs"></param>
        /// <param name="pDescs"></param>
        /// <returns></returns>
        private static Request Create(CliOptions pOptions, IEnumerable<string> pArgs, IEnumerable<Description> pDescs)
        {
            return Create(pOptions, new Validator(new ConsoleOutput(pOptions)), pArgs, pDescs.ToList());
        }

        /// <summary>
        /// </summary>
        /// <param name="pOptions"></param>
        /// <param name="pValidator"></param>
        /// <param name="pArgs"></param>
        /// <param name="pDescs"></param>
        /// <returns></returns>
        private static Request Create(CliOptions pOptions, iValidator pValidator, IEnumerable<string> pArgs,
                                      ICollection<Description> pDescs)
        {
            IEnumerable<Argument> arguments = ArgumentFactory.Create(pOptions.Prefix, pOptions.EqualChar, pArgs);
            Request request = new Request(arguments, pDescs);
            if (pValidator != null)
            {
                request.Valid = pValidator.Validate(pDescs.ToList(), request);
            }
            return request;
        }

        /// <summary>
        /// Creates descriptions of parameters from the public property members of an
        /// object.
        /// </summary>
        /// <typeparam name="T">The object type to inspect.</typeparam>
        /// <param name="pOptions">The parser option</param>
        /// <param name="pInfos">Collection of properties</param>
        /// <returns>Parameter descriptions in syntax format.</returns>
        private static string ReflectDescriptions<T>(CliOptions pOptions, IEnumerable<PropertyInfo> pInfos)
            where T : class, new()
        {
            List<string> desc = new List<string>();
            foreach (PropertyInfo info in pInfos)
            {
                eROLE role = eROLE.PASSED;
                string name = info.Name.ToLower();

                CliName attrName = Attribute<CliName>(info);
                if (attrName != null)
                {
                    role = attrName.Role;
                    name = attrName.Name ?? name;
                }

                desc.Add(
                    string.Format(
                        "{0}{1}:{2}",
                        role == eROLE.NAMED ? pOptions.Prefix : "",
                        name,
                        info.PropertyType.Name.ToLower()
                        ));
            }
            return string.Join(" ", desc);
        }

        /// <summary>
        /// Finds a property info using a lowercase match on the name.
        /// </summary>
        /// <param name="pInfos">The properties to search.</param>
        /// <param name="pName">The name to find.</param>
        /// <returns>The property of null.</returns>
        private static PropertyInfo ReflectProperty(IEnumerable<PropertyInfo> pInfos, string pName)
        {
            return pInfos.FirstOrDefault(pInfo=>pInfo.Name.ToLower() == pName);
        }

        /// <summary>
        /// An easy factory method that uses an object's properties to
        /// define the command line arguments required for the application.
        /// </summary>
        /// <typeparam name="T">The object class to initialize.</typeparam>
        /// <param name="pOptions">The parser options</param>
        /// <param name="pArgs">The command line arguments.</param>
        /// <returns>A new class T with it's properties populated, or Null if command line arguments are invalid.</returns>
        public static T Create<T>(CliOptions pOptions, IEnumerable<string> pArgs) where T : class, new()
        {
            PropertyInfo[] infos = typeof (T).GetProperties();
            string pattern = ReflectDescriptions<T>(pOptions, infos);
            List<Description> descs = DescriptionFactory.Create(pOptions, new HelpReflection(typeof (T)), pattern);

            Request request = Create(pArgs, descs);
            if (!request.Valid)
            {
                return null;
            }

            T instance = Activator.CreateInstance<T>();

            foreach (Description desc in descs)
            {
                PropertyInfo info = ReflectProperty(infos, desc.Name);
                if (!request.Contains(desc.Name) || info == null)
                {
                    continue;
                }
                // TODO: Support multiples
                info.SetValue(instance, Convert.ChangeType(request.First(desc.Name).Value, info.PropertyType));
            }

            return instance;
        }

        /// <summary>
        /// Creates a request object from command line arguments, and a collection of argument descriptions.
        /// </summary>
        /// <param name="pArgs">The command line arguments.</param>
        /// <param name="pDescs">The argument descriptions.</param>
        /// <returns>The request object.</returns>
        public static Request Create(IEnumerable<string> pArgs, IEnumerable<Description> pDescs)
        {
            return Create(CliOptions.WindowsStyle, pArgs, pDescs.ToList());
        }
    }
}