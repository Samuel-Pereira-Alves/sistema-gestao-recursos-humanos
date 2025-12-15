using System.Text.RegularExpressions;
using sistema_gestao_recursos_humanos.backend.models.tools;

namespace sistema_gestao_recursos_humanos.backend.models.tools
{
    public static class ResumeParser
    {
        public static ResumeData ParseFromText(string text)
        {
            var data = new ResumeData();

            var lines = text.Split('\n')
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            // Email
            var emailMatch = Regex.Match(text, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");
            if (emailMatch.Success) data.Email = emailMatch.Value;

            // Name (naive: first non-empty line, split by spaces)
            if (lines.Count > 0)
            {
                var nameLine = lines[0];
                var parts = nameLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    data.FirstName = parts[0];
                    data.LastName = parts[^1];
                }
            }

            // Skills / Summary / Perfil
            var skillsRegex = new Regex(@"(?is)(skills|competências|summary|perfil)\s*[:\-]?\s*(.+?)(?:\n\s*\n|$)");
            var skillsMatch = skillsRegex.Match(text);
            if (skillsMatch.Success)
            {
                data.Skills = skillsMatch.Groups[2].Value.Trim();
            }
            else
            {
                // fallback: first 5-8 lines after name
                data.Skills = string.Join(" ", lines.Skip(1).Take(8));
            }

            // Employment: try blocks like "YYYY(-MM)? – YYYY(-MM)? Company ..."
            var empRegex = new Regex(@"(?im)^(?<start>\d{4}(?:-\d{2})?)\s*[–-]\s*(?<end>\d{4}(?:-\d{2})?|present|actual)\s*(?<rest>.+)$");
            foreach (Match m in empRegex.Matches(text))
            {
                var startStr = m.Groups["start"].Value;
                var endStr = m.Groups["end"].Value;
                var rest = m.Groups["rest"].Value.Trim();

                DateTime? start = TryParseDate(startStr);
                DateTime? end = TryParseDate(endStr);

                // Try to extract org and title (very heuristic)
                var orgMatch = Regex.Match(rest, @"(?i)(company|empresa|org|organization|employer)\s*[:\-]\s*(.+)");
                var titleMatch = Regex.Match(rest, @"(?i)(title|cargo|job\s*title|função)\s*[:\-]\s*(.+)");

                var org = orgMatch.Success ? orgMatch.Groups[2].Value.Trim() : "Unknown";
                var title = titleMatch.Success ? titleMatch.Groups[2].Value.Trim() : "Unknown";

                data.Employment.Add(new EmploymentItem(
                    StartDate: start ?? new DateTime(2020, 1, 1),
                    EndDate: end ?? new DateTime(2024, 1, 1),
                    OrgName: org,
                    JobTitle: title,
                    Responsibility: rest,
                    FunctionCategory: "N/A",
                    IndustryCategory: "N/A",
                    CountryRegion: "N/A",
                    State: "N/A",
                    City: "N/A"
                ));
            }

            // Ensure at least one Employment (AdventureWorks schema tends to require it)
            if (data.Employment.Count == 0)
            {
                data.Employment.Add(new EmploymentItem(
                    StartDate: new DateTime(2020, 1, 1),
                    EndDate: new DateTime(2024, 1, 1),
                    OrgName: "Unknown",
                    JobTitle: "Unknown",
                    Responsibility: "N/A",
                    FunctionCategory: "N/A",
                    IndustryCategory: "N/A",
                    CountryRegion: "N/A",
                    State: "N/A",
                    City: "N/A"
                ));
            }

            // Education: find a block with "Education / Formação"
            var eduRegex = new Regex(@"(?is)(education|educação|formação)\s*[:\-]?\s*(.+?)(?:\n\s*\n|$)");
            var eduMatch = eduRegex.Match(text);
            if (eduMatch.Success)
            {
                var eduBlock = eduMatch.Groups[2].Value;

                var degreeMatch = Regex.Match(eduBlock, @"(?i)(Bachelor|Licenciatura|Master|Mestrado|PhD|Doutoramento)");
                if (degreeMatch.Success) data.EduDegree = degreeMatch.Value;

                var schoolMatch = Regex.Match(eduBlock, @"(?i)(University|Universidade|Institute|Instituto)\s+([^\n]+)");
                if (schoolMatch.Success) data.EduSchool = schoolMatch.Value;
            }

            // Telephone
            var telMatch = Regex.Match(text, @"(\+?\d[\d\s\-]{7,})");
            if (telMatch.Success) data.TelNumber = telMatch.Value.Trim();

            return data;
        }

        private static DateTime? TryParseDate(string s)
        {
            s = s.Trim();
            if (string.Equals(s, "present", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "actual", StringComparison.OrdinalIgnoreCase))
                return new DateTime(2024, 1, 1); // fallback end date

            if (DateTime.TryParse(s, out var d)) return d;

            // yyyy
            if (Regex.IsMatch(s, @"^\d{4}$") && int.TryParse(s, out var year))
                return new DateTime(year, 1, 1);

            // yyyy-MM
            if (Regex.IsMatch(s, @"^\d{4}-\d{2}$"))
            {
                var parts = s.Split('-');
                if (int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m))
                    return new DateTime(y, m, 1);
            }

            return null;
        }
    }
}