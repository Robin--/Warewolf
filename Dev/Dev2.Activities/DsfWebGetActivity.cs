﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Dev2.Common;
using Dev2.Common.Common;
using Dev2.Common.Interfaces;
using Dev2.Common.Interfaces.Core.Graph;
using Dev2.Common.Interfaces.Data;
using Dev2.Common.Interfaces.ServerProxyLayer;
using Dev2.Common.Interfaces.Toolbox;
using Dev2.Data.Util;
using Dev2.DataList.Contract;
using Dev2.Runtime.Hosting;
using Dev2.Runtime.ServiceModel.Data;
using Unlimited.Applications.BusinessDesignStudio.Activities;
using Unlimited.Framework.Converters.Graph;
using Unlimited.Framework.Converters.Graph.Ouput;
using Warewolf.Core;
using WarewolfParserInterop;

namespace Dev2
{
    [ToolDescriptorInfo("Resources-Service", "GET Web Service", ToolType.Native, "6AEB1038-6332-46F9-8BDD-641DE4EA038E", "Dev2.Acitivities", "1.0.0.0", "Legacy", "Resources", "/Warewolf.Studio.Themes.Luna;component/Images.xaml")]
    public class DsfWebGetActivity : DsfActivity
    {

        public IList<INameValue> Headers { get; set; }

        public string QueryString { get; set; }

        public IWebServiceSource SavedSource { get; set; }

        public IOutputDescription OutputDescription { get; set; }


        #region Overrides of DsfNativeActivity<bool>


        protected override void ExecutionImpl(IEsbChannel esbChannel, IDSFDataObject dataObject, string inputs, string outputs, out ErrorResultTO errors, int update)
        {
            errors = new ErrorResultTO();
            if (Headers == null)
            {
                errors.AddError("Headers Are Null");
                return;
            }
            if (QueryString == null)
            {
                errors.AddError("Query is Null");
                return;
            }
            var head = Headers.Select(a => new NameValue(dataObject.Environment.Eval(a.Name, update).ToString(), dataObject.Environment.Eval(a.Value, update).ToString()));
            var query = dataObject.Environment.Eval(QueryString,update);
            var url = ResourceCatalog.Instance.GetResource<WebSource>(Guid.Empty, SourceId);
            var client = CreateClient(head, query, url);
            var result = client.DownloadString(url.Address+query);
            //ExecuteService(update, out errors, Method, Namespace, dataObject, OutputFormatterFactory.CreateOutputFormatter(OutputDescription));
            DataSourceShape shape = new DataSourceShape(){Paths = Outputs.Select(a=>((ServiceOutputMapping)a).Path).ToList()};
            PushXmlIntoEnvironment(result, update,dataObject);
        }

        public void PushXmlIntoEnvironment(string input, int update, IDSFDataObject dataObj)
        {
            int i = 0;
            foreach(var serviceOutputMapping in Outputs)
            {
                OutputDescription.DataSourceShapes[0].Paths[i].OutputExpression = serviceOutputMapping.MappedTo;
                i++;
            }

            var formater = OutputFormatterFactory.CreateOutputFormatter(OutputDescription);

            input = formater.Format(input).ToString();
            if (input != string.Empty)
            {
                try
                {
                    string toLoad = DataListUtil.StripCrap(input); // clean up the rubish ;)
                    XmlDocument xDoc = new XmlDocument();
                    toLoad = string.Format("<Tmp{0}>{1}</Tmp{0}>", Guid.NewGuid().ToString("N"), toLoad);
                    xDoc.LoadXml(toLoad);

                    if (xDoc.DocumentElement != null)
                    {
                        XmlNodeList children = xDoc.DocumentElement.ChildNodes;
                        IDictionary<string, int> indexCache = new Dictionary<string, int>();
                        var outputDefs = Outputs.Select(a => new Dev2Definition(a.MappedFrom, a.MappedTo, "", a.RecordSetName, true, "", true, a.MappedTo) as IDev2Definition).ToList();
                        TryConvert(children, outputDefs, indexCache, update, dataObj,0);
                    }
                }
                catch (Exception e)
                {
                    Dev2Logger.Error(e.Message, e);
                }
            }
        }

        void TryConvert(XmlNodeList children, IList<IDev2Definition> outputDefs, IDictionary<string, int> indexCache, int update, IDSFDataObject dataObj, int level = 0)
        {
            // spin through each element in the XML
            foreach (XmlNode c in children)
            {
                if (c.Name != GlobalConstants.NaughtyTextNode)
                {
                    // scalars and recordset fetch
                    if (level > 0)
                    {
                        var c1 = c;
                        var recSetName = outputDefs.Where(definition => definition.RecordSetName == c1.Name);
                        var dev2Definitions = recSetName as IDev2Definition[] ?? recSetName.ToArray();
                        if (dev2Definitions.Length != 0)
                        {
                            // fetch recordset index
                            int fetchIdx;
                            var idx = indexCache.TryGetValue(c.Name, out fetchIdx) ? fetchIdx : 1;
                            // process recordset
                            var nl = c.ChildNodes;
                            foreach (XmlNode subc in nl)
                            {
                                // Extract column being mapped to ;)
                                foreach (var definition in dev2Definitions)
                                {
                                    if (definition.MapsTo == subc.Name || definition.Name == subc.Name)
                                    {
                                        dataObj.Environment.AssignWithFrame(new AssignValue(definition.RawValue, subc.InnerXml), update);
                                    }
                                }
                            }
                            // update this recordset index
                            dataObj.Environment.CommitAssign();
                            indexCache[c.Name] = ++idx;
                        }
                        else
                        {
                            var scalarName = outputDefs.FirstOrDefault(definition => definition.Name == c1.Name);
                            if (scalarName != null)
                            {
                                dataObj.Environment.AssignWithFrame(new AssignValue(DataListUtil.AddBracketsToValueIfNotExist(scalarName.RawValue), UnescapeRawXml(c1.InnerXml)), update);
                            }
                        }
                    }
                    else
                    {
                        if (level == 0)
                        {
                            // Only recurse if we're at the first level!!
                            TryConvert(c.ChildNodes, outputDefs, indexCache, update, dataObj, ++level);
                        }
                    }
                }
            }
        }

        string UnescapeRawXml(string innerXml)
        {
            if (innerXml.StartsWith("&lt;") && innerXml.EndsWith("&gt;"))
            {
                return new StringBuilder(innerXml).Unescape().ToString();
            }
            return innerXml;
        }


        private WebClient CreateClient(IEnumerable<NameValue> head, WarewolfDataEvaluationCommon.WarewolfEvalResult query, WebSource source)
        {
            var webclient = new WebClient();
            foreach(var nameValue in head)
            {
                webclient.Headers.Add(nameValue.Name,nameValue.Value);
            }

            if (source.AuthenticationType == AuthenticationType.User)
            {
                webclient.Credentials = new NetworkCredential(source.UserName, source.Password);
            }

            var contentType = webclient.Headers["Content-Type"];
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/x-www-form-urlencoded";
            }
            webclient.Headers["Content-Type"] = contentType;
            webclient.Headers.Add("user-agent", GlobalConstants.UserAgentString);
            webclient.BaseAddress = source.Address + query;
            return webclient;
        }

        #endregion

        public DsfWebGetActivity()
        {
            Type = "Web Get Request Connector";
            DisplayName = "Web Get Request Connector";
        }

    }
}