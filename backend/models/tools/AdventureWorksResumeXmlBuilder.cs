using System.Xml.Linq;
using sistema_gestao_recursos_humanos.backend.models.tools;

namespace sistema_gestao_recursos_humanos.backend.models.tools
{
    public static class AdventureWorksResumeXmlBuilder
    {
        private static readonly XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/Resume";

        public static string Build(ResumeData data)
        {
            var root = new XElement(ns + "Resume",
                new XAttribute(XNamespace.Xmlns + "ns", ns.NamespaceName),

                // 1. Name
                new XElement(ns + "Name",
                    new XElement(ns + "Name.Prefix"),
                    new XElement(ns + "Name.First", SafeNonEmpty(data.FirstName, "Unknown")),
                    new XElement(ns + "Name.Middle"),
                    new XElement(ns + "Name.Last", SafeNonEmpty(data.LastName, "Unknown")),
                    new XElement(ns + "Name.Suffix")
                ),

                // 2. Skills
                new XElement(ns + "Skills", SafeNonEmpty(data.Skills, "N/A")),

                // 3. Employment (at least 1)
                data.Employment.Select(emp => new XElement(ns + "Employment",
                    new XElement(ns + "Emp.StartDate", FormatDate(emp.StartDate ?? new DateTime(2020, 1, 1))),
                    new XElement(ns + "Emp.EndDate", FormatDate(emp.EndDate ?? new DateTime(2024, 1, 1))),
                    new XElement(ns + "Emp.OrgName", SafeNonEmpty(emp.OrgName, "Unknown")),
                    new XElement(ns + "Emp.JobTitle", SafeNonEmpty(emp.JobTitle, "Unknown")),
                    new XElement(ns + "Emp.Responsibility", SafeNonEmpty(emp.Responsibility, "N/A")),
                    new XElement(ns + "Emp.FunctionCategory", SafeNonEmpty(emp.FunctionCategory, "N/A")),
                    new XElement(ns + "Emp.IndustryCategory", SafeNonEmpty(emp.IndustryCategory, "N/A")),
                    new XElement(ns + "Emp.Location",
                        new XElement(ns + "Location",
                            new XElement(ns + "Loc.CountryRegion", SafeNonEmpty(emp.CountryRegion, "N/A")),
                            new XElement(ns + "Loc.State", SafeNonEmpty(emp.State, "N/A")),
                            new XElement(ns + "Loc.City", SafeNonEmpty(emp.City, "N/A"))
                        )
                    )
                )),

                // 4. Education (your schema seems to require it with non-empty GPA/GPAScale)
                new XElement(ns + "Education",
                    new XElement(ns + "Edu.Level", SafeNonEmpty(data.EduLevel, "Bachelor")),
                    new XElement(ns + "Edu.StartDate", FormatDate(data.EduStart ?? new DateTime(2020, 1, 1))),
                    new XElement(ns + "Edu.EndDate", FormatDate(data.EduEnd ?? new DateTime(2024, 1, 1))),
                    new XElement(ns + "Edu.Degree", SafeNonEmpty(data.EduDegree, "Unknown")),
                    new XElement(ns + "Edu.Major", SafeNonEmpty(data.EduMajor, "Unknown")),
                    new XElement(ns + "Edu.Minor", SafeNonEmpty(data.EduMinor, "Unknown")),
                    new XElement(ns + "Edu.GPA", SafeNonEmpty(data.EduGPA, "0.0")),
                    new XElement(ns + "Edu.GPAScale", SafeNonEmpty(data.EduGPAScale, "4")),
                    new XElement(ns + "Edu.School", SafeNonEmpty(data.EduSchool, "Unknown")),
                    new XElement(ns + "Edu.Location",
                        new XElement(ns + "Location",
                            new XElement(ns + "Loc.CountryRegion", SafeNonEmpty(data.CountryRegion, "N/A")),
                            new XElement(ns + "Loc.State", SafeNonEmpty(data.State, "N/A")),
                            new XElement(ns + "Loc.City", SafeNonEmpty(data.City, "N/A"))
                        )
                    )
                ),

                // 5. Address
                new XElement(ns + "Address",
                    new XElement(ns + "Addr.Type", "Home"),
                    new XElement(ns + "Addr.Street", "N/A"),
                    new XElement(ns + "Addr.Location",
                        new XElement(ns + "Location",
                            new XElement(ns + "Loc.CountryRegion", SafeNonEmpty(data.CountryRegion, "N/A")),
                            new XElement(ns + "Loc.State", SafeNonEmpty(data.State, "N/A")),
                            new XElement(ns + "Loc.City", SafeNonEmpty(data.City, "N/A"))
                        )
                    ),
                    new XElement(ns + "Addr.PostalCode", "0000-000"),
                    new XElement(ns + "Addr.Telephone",
                        new XElement(ns + "Telephone",
                            new XElement(ns + "Tel.Type", "Voice"),
                            new XElement(ns + "Tel.IntlCode", "351"),
                            new XElement(ns + "Tel.AreaCode", "21"),
                            new XElement(ns + "Tel.Number", SafeNonEmpty(data.TelNumber, "0000000"))
                        )
                    )
                ),

                // 6. EMail
                new XElement(ns + "EMail", SafeNonEmpty(data.Email, "unknown@example.com")),

                // 7. WebSite
                new XElement(ns + "WebSite", "")
            );

            var doc = new XDocument(root);
            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private static string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd'Z'");
        private static string SafeNonEmpty(string? s, string fallback) => string.IsNullOrWhiteSpace(s) ? fallback : s!;
    }
}