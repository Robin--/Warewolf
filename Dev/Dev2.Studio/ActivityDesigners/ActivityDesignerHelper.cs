/*
*  Warewolf - Once bitten, there's no going back
*  Copyright 2016 by Warewolf Ltd <alpha@warewolf.io>
*  Licensed under GNU Affero General Public License 3.0 or later.
*  Some rights reserved.
*  Visit our website for more information <http://warewolf.io/>
*  AUTHORS <http://warewolf.io/authors.php> , CONTRIBUTORS <http://warewolf.io/contributors.php>
*  @license GNU Affero General Public License <http://www.gnu.org/licenses/agpl-3.0.html>
*/

using Dev2.Activities;
using Dev2.Activities.Designers2.AggregateCalculate;
using Dev2.Activities.Designers2.BaseConvert;
using Dev2.Activities.Designers2.Calculate;
using Dev2.Activities.Designers2.CaseConvert;
using Dev2.Activities.Designers2.CommandLine;
using Dev2.Activities.Designers2.Comment;
using Dev2.Activities.Designers2.Copy;
using Dev2.Activities.Designers2.CountRecords;
using Dev2.Activities.Designers2.Create;
using Dev2.Activities.Designers2.CreateJSON;
using Dev2.Activities.Designers2.DataMerge;
using Dev2.Activities.Designers2.DataSplit;
using Dev2.Activities.Designers2.DateTime;
using Dev2.Activities.Designers2.DateTimeDifference;
using Dev2.Activities.Designers2.Delete;
using Dev2.Activities.Designers2.DeleteRecords;
using Dev2.Activities.Designers2.DropBox2016.Delete;
using Dev2.Activities.Designers2.DropBox2016.Download;
using Dev2.Activities.Designers2.DropBox2016.DropboxFile;
using Dev2.Activities.Designers2.DropBox2016.Upload;
using Dev2.Activities.Designers2.Email;
using Dev2.Activities.Designers2.ExchangeEmail;
using Dev2.Activities.Designers2.FindIndex;
using Dev2.Activities.Designers2.FindRecordsMultipleCriteria;
using Dev2.Activities.Designers2.Foreach;
using Dev2.Activities.Designers2.FormatNumber;
using Dev2.Activities.Designers2.GatherSystemInformation;
using Dev2.Activities.Designers2.GetWebRequest;
using Dev2.Activities.Designers2.GetWebRequest.GetWebRequestWithTimeout;
using Dev2.Activities.Designers2.Move;
using Dev2.Activities.Designers2.MultiAssign;
using Dev2.Activities.Designers2.MySqlDatabase;
using Dev2.Activities.Designers2.Net_DLL;
using Dev2.Activities.Designers2.ODBC;
using Dev2.Activities.Designers2.Oracle;
using Dev2.Activities.Designers2.PostgreSql;
using Dev2.Activities.Designers2.RabbitMQ.Consume;
using Dev2.Activities.Designers2.RabbitMQ.Publish;
using Dev2.Activities.Designers2.Random;
using Dev2.Activities.Designers2.ReadFile;
using Dev2.Activities.Designers2.ReadFolder;
using Dev2.Activities.Designers2.RecordsLength;
using Dev2.Activities.Designers2.Rename;
using Dev2.Activities.Designers2.Replace;
using Dev2.Activities.Designers2.Script;
using Dev2.Activities.Designers2.SelectAndApply;
using Dev2.Activities.Designers2.Sequence;
using Dev2.Activities.Designers2.Service;
using Dev2.Activities.Designers2.SharePointCopyFile;
using Dev2.Activities.Designers2.SharePointDeleteFile;
using Dev2.Activities.Designers2.SharePointFileDownload;
using Dev2.Activities.Designers2.SharePointFileUpload;
using Dev2.Activities.Designers2.SharepointFolderRead;
using Dev2.Activities.Designers2.SharepointListCreate;
using Dev2.Activities.Designers2.SharepointListDelete;
using Dev2.Activities.Designers2.SharepointListRead;
using Dev2.Activities.Designers2.SharepointListUpdate;
using Dev2.Activities.Designers2.SharePointMoveFile;
using Dev2.Activities.Designers2.SortRecords;
using Dev2.Activities.Designers2.SqlBulkInsert;
using Dev2.Activities.Designers2.SqlServerDatabase;
using Dev2.Activities.Designers2.UniqueRecords;
using Dev2.Activities.Designers2.Unzip;
using Dev2.Activities.Designers2.WCFEndPoint;
using Dev2.Activities.Designers2.Web_Service_Delete;
using Dev2.Activities.Designers2.Web_Service_Post;
using Dev2.Activities.Designers2.Web_Service_Put;
using Dev2.Activities.Designers2.WebServiceGet;
using Dev2.Activities.Designers2.WriteFile;
using Dev2.Activities.Designers2.XPath;
using Dev2.Activities.Designers2.Zip;
using Dev2.Activities.DropBox2016.DeleteActivity;
using Dev2.Activities.DropBox2016.DownloadActivity;
using Dev2.Activities.DropBox2016.DropboxFileActivity;
using Dev2.Activities.DropBox2016.UploadActivity;
using Dev2.Activities.Exchange;
using Dev2.Activities.RabbitMQ.Consume;
using Dev2.Activities.RabbitMQ.Publish;
using Dev2.Activities.SelectAndApply;
using Dev2.Activities.Sharepoint;
using Dev2.Activities.WcfEndPoint;
using Dev2.Studio.ViewModels.Workflow;
using System;
using System.Collections.Generic;
using Dev2.Activities.Designers2.ComDLL;
using Unlimited.Applications.BusinessDesignStudio.Activities;
using Dev2.Activities.Designers2.MultiAssignObject;

// ReSharper disable CheckNamespace
namespace Dev2.Studio.ActivityDesigners
{
    public static class ActivityDesignerHelper
    {
        static readonly Dictionary<Type, Type> DesignerAttributes = new Dictionary<Type, Type>
            {
                { typeof(DsfMultiAssignActivity), typeof(MultiAssignDesigner) },
                { typeof(DsfMultiAssignObjectActivity), typeof(MultiAssignObjectDesigner) },
                { typeof(DsfDateTimeActivity), typeof(DateTimeDesigner) },
                { typeof(DsfWebGetRequestWithTimeoutActivity), typeof(GetWebRequestWithTimeOutDesigner) },
                { typeof(DsfWebGetRequestActivity), typeof(GetWebRequestDesigner) },
                { typeof(DsfFindRecordsMultipleCriteriaActivity), typeof(FindRecordsMultipleCriteriaDesigner) },
                { typeof(DsfSqlBulkInsertActivity), typeof(SqlBulkInsertDesigner) },
                { typeof(DsfSortRecordsActivity), typeof(SortRecordsDesigner) },
                { typeof(DsfCountRecordsetActivity), typeof(CountRecordsDesigner) },
                { typeof(DsfRecordsetLengthActivity), typeof(RecordsLengthDesigner) },
                { typeof(DsfDeleteRecordActivity), typeof(DeleteRecordsDesigner) },
                { typeof(DsfUniqueActivity), typeof(UniqueRecordsDesigner) },
                { typeof(DsfCalculateActivity), typeof(CalculateDesigner) },
                { typeof(DsfAggregateCalculateActivity), typeof(AggregateCalculateDesigner) },
                { typeof(DsfBaseConvertActivity), typeof(BaseConvertDesigner) },
                { typeof(DsfNumberFormatActivity), typeof(FormatNumberDesigner) },
                { typeof(DsfPathCopy), typeof(CopyDesigner) },
                { typeof(DsfPathCreate), typeof(CreateDesigner) },
                { typeof(DsfPathMove), typeof(MoveDesigner) },
                { typeof(DsfPathDelete), typeof(DeleteDesigner) },
                { typeof(DsfFileRead), typeof(ReadFileDesigner) },
                { typeof(DsfFileWrite), typeof(WriteFileDesigner) },
                { typeof(DsfFolderRead), typeof(ReadFolderDesigner) },
                { typeof(DsfPathRename), typeof(RenameDesigner) },
                { typeof(DsfUnZip), typeof(UnzipDesigner) },
                { typeof(DsfZip), typeof(ZipDesigner) },
                { typeof(DsfExecuteCommandLineActivity), typeof(CommandLineDesigner) },
                { typeof(DsfCommentActivity), typeof(CommentDesigner) },
                { typeof(DsfSequenceActivity), typeof(SequenceDesigner) },
                { typeof(DsfDateTimeDifferenceActivity), typeof(DateTimeDifferenceDesigner) },
                { typeof(DsfSendEmailActivity), typeof(EmailDesigner) },
                { typeof(DsfIndexActivity), typeof(FindIndexDesigner) },
                { typeof(DsfRandomActivity), typeof(RandomDesigner) },
                { typeof(DsfReplaceActivity), typeof(ReplaceDesigner) },
                { typeof(DsfScriptingActivity), typeof(ScriptDesigner) },
                { typeof(DsfForEachActivity), typeof(ForeachDesigner) },
                { typeof(DsfCaseConvertActivity), typeof(CaseConvertDesigner) },
                { typeof(DsfDataMergeActivity), typeof(DataMergeDesigner) },
                { typeof(DsfDataSplitActivity), typeof(DataSplitDesigner) },
                { typeof(DsfGatherSystemInformationActivity), typeof(GatherSystemInformationDesigner) },
                { typeof(DsfXPathActivity), typeof(XPathDesigner) },
                { typeof(DsfActivity), typeof(ServiceDesigner) },
                { typeof(DsfSqlServerDatabaseActivity), typeof(SqlServerDatabaseDesigner) },
                { typeof(DsfMySqlDatabaseActivity), typeof(MySqlDatabaseDesigner) },
                { typeof(DsfOracleDatabaseActivity), typeof(OracleDatabaseDesigner) },
                  { typeof(DsfODBCDatabaseActivity), typeof(ODBCDatabaseDesigner) },
                  { typeof(DsfPostgreSqlActivity), typeof(PostgreSqlDatabaseDesigner) },
                {typeof(DsfExchangeEmailActivity),typeof(ExchangeEmailDesigner) },
                { typeof(DsfDotNetDllActivity), typeof(DotNetDllDesigner) },
                { typeof(DsfComDllActivity), typeof(ComDllDesigner) },
                { typeof(DsfWebGetActivity), typeof(WebServiceGetDesigner) },
                { typeof(DsfWebPostActivity), typeof(WebServicePostDesigner) },
                { typeof(DsfWebDeleteActivity), typeof(WebServiceDeleteDesigner) },
                { typeof(DsfWebPutActivity), typeof(WebServicePutDesigner) },
                { typeof(DsfDropBoxUploadActivity), typeof(DropBoxUploadDesigner) },
                { typeof(DsfDropBoxDownloadActivity), typeof(DropBoxDownloadDesigner) },
                 { typeof(DsfDropBoxDeleteActivity), typeof(DropBoxDeleteDesigner) },
                 { typeof(DsfDropboxFileListActivity), typeof(DropBoxFileListDesigner) },
                { typeof(DsfWebserviceActivity), typeof(ServiceDesigner) },
                { typeof(DsfPluginActivity), typeof(ServiceDesigner) },
                { typeof(DsfCreateJsonActivity), typeof(CreateJsonDesigner) },
                { typeof(SharepointReadListActivity), typeof(SharepointListReadDesigner) },
                { typeof(SharepointCreateListItemActivity), typeof(SharepointListCreateDesigner) },
                { typeof(SharepointDeleteListItemActivity), typeof(SharepointListDeleteDesigner) },
                { typeof(SharepointUpdateListItemActivity), typeof(SharepointListUpdateDesigner) },
                { typeof(SharepointReadFolderItemActivity), typeof(SharePointReadFolderDesigner) },
                { typeof(SharepointFileUploadActivity), typeof(SharePointFileUploadDesigner) },
                { typeof(SharepointFileDownLoadActivity), typeof(SharePointFileDownLoadDesigner) },
                { typeof(SharepointCopyFileActivity), typeof(SharePointCopyFileDesigner) },
                { typeof(SharepointDeleteFileActivity), typeof(SharePointDeleteFileDesigner) },
                { typeof(SharepointMoveFileActivity), typeof(SharePointMoveFileDesigner) },
                { typeof(DsfWcfEndPointActivity),typeof(WcfEndPointDesigner)},
                { typeof(DsfPublishRabbitMQActivity), typeof(RabbitMQPublishDesigner) },
                { typeof(DsfSelectAndApplyActivity), typeof(SelectAndApplyDesigner) },
                { typeof(DsfConsumeRabbitMQActivity), typeof(RabbitMQConsumeDesigner) },
            };
        public static void AddDesignerAttributes(WorkflowDesignerViewModel workflowVm, bool liteInit = false)
        {
            workflowVm.InitializeDesigner(DesignerAttributes, liteInit);
        }
    }
}
