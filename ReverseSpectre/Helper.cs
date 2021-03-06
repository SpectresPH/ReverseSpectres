﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ReverseSpectre.Models;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace ReverseSpectre
{
    public class Helper
    {
        public class StorageHelper
        {
            /// <summary>
            /// Uploads file to specified blob container.
            /// </summary>
            /// <param name="storageConnectionString">Connection string to storage account.</param>
            /// <param name="containerName">Blob container name, will create if it does not exist.</param>
            /// <param name="fileStream">MemoryStream for file upload.</param>
            /// <param name="fileName">Name of file on blob.</param>
            /// <returns>Returns the URL of the uploaded file.</returns>
            public static async Task<string> UploadToStorage(string storageConnectionString, string containerName, Stream fileStream, string fileName)
            {
                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create the Blob Client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                
                // Retrieve a reference to a Container.
                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
                // Create the Container if it doesn't already exist.
                container.CreateIfNotExists();
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                
                // Upload file
                await blob.UploadFromStreamAsync(fileStream);
                return blob.Uri.ToString();
            }
        }

        public class Email
        {
            /// <summary>
            /// Sends html email to specified email address.
            /// </summary>
            /// <param name="email">Recipient email address.</param>
            /// <param name="subject">Email subject.</param>
            /// <param name="body">Email htm body content.</param>
            /// <returns></returns>
            public static async Task SendEmailAsync(string email, string subject, string body)
            {
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    // Set smtp credentials
                    smtp.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["RegistrationEmail"],
                        ConfigurationManager.AppSettings["RegistrationEmailPassword"]);
                    smtp.EnableSsl = true;
                    string emailFrom = ConfigurationManager.AppSettings["RegistrationEmail"];
                    // Create mail
                    using (MailMessage mail = new MailMessage(emailFrom, email))
                    {
                        mail.Subject = subject;
                        mail.Body = body;
                        mail.IsBodyHtml = true;
                        await smtp.SendMailAsync(mail);
                    }
                }
            }
        }

        public class SelectListHelper
        {
            public static IEnumerable<SelectListItem> SourceOfFundsList
            {
                get
                {
                    List<SelectListItem> list = new List<SelectListItem>();
                    foreach (var item in Enum.GetValues(typeof(SourceOfFundsType)))
                    {
                        list.Add(new SelectListItem()
                        {
                            Text = item.GetType().GetMember(item.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name,
                            Value = ((int)item).ToString()
                        });
                    }
                    return list;
                }
            }

            public IEnumerable<SelectListItem> FormOfBusinessList
            {
                get
                {
                    List<SelectListItem> list = new List<SelectListItem>();
                    foreach (var item in Enum.GetValues(typeof(FormOfBusinessType)))
                    {
                        list.Add(new SelectListItem()
                        {
                            Text = item.GetType().GetMember(item.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name,
                            Value = ((int)item).ToString()
                        });
                    }
                    return list;
                }
            }

        }

        public class LoanRequirements
        {
            public static List<LoanApplicationDocument> GetBasicLoanRequirements(LoanApplication application)
            {
                // Set required documents
                List<LoanApplicationDocument> documents = new List<LoanApplicationDocument>();
                documents.Add(new LoanApplicationDocument()
                {
                    Name = "Certificate of Registration",
                    Comment = "",
                    TimestampCreated = DateTime.Now,
                    Status = LoanDocumentStatusType.Pending,
                    LoanApplication = application
                });
                documents.Add(new LoanApplicationDocument()
                {
                    Name = $"Financial Statement - {DateTime.Today.Year}",
                    Comment = "",
                    TimestampCreated = DateTime.Now,
                    Status = LoanDocumentStatusType.Pending,
                    LoanApplication = application
                });
                return documents;
            }
        }


    }

    public static class HtmlHelpers
    {
        public static string MakeActiveClass(this UrlHelper urlHelper, string controller)
        {
            string result = "active";
            string controllerName = urlHelper.RequestContext.RouteData.Values["controller"].ToString();

            if (!controllerName.Equals(controller, StringComparison.OrdinalIgnoreCase))
            {
                result = null;
            }

            return result;
        }

        public static string SetClass(this HtmlHelper htmlHelper, bool condition, string className)
        {
            if (condition)
                return className;
            else
                return null;
        }

        public static string GetControllerName(this UrlHelper urlHelper)
        {
            return urlHelper.RequestContext.RouteData.Values["controller"].ToString();
        }
    }

    public static class EnumHelper<T>
    {
        private static string lookupResource(Type resourceManagerProvider, string resourceKey)
        {
            foreach (PropertyInfo staticProperty in resourceManagerProvider.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (staticProperty.PropertyType == typeof(System.Resources.ResourceManager))
                {
                    System.Resources.ResourceManager resourceManager = (System.Resources.ResourceManager)staticProperty.GetValue(null, null);
                    return resourceManager.GetString(resourceKey);
                }
            }

            return resourceKey;
        }

        public static string GetDisplayValue(T value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var descriptionAttributes = fieldInfo.GetCustomAttributes(
                typeof(DisplayAttribute), false) as DisplayAttribute[];

            if (descriptionAttributes[0].ResourceType != null)
                return lookupResource(descriptionAttributes[0].ResourceType, descriptionAttributes[0].Name);

            if (descriptionAttributes == null) return string.Empty;
            return (descriptionAttributes.Length > 0) ? descriptionAttributes[0].Name : value.ToString();
        }
    }
}