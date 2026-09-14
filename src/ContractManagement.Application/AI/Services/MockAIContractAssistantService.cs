using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ContractManagement.Application.AI.Services;

/// <summary>
/// Mock AI Contract Assistant Service for MVP development and testing.
///
/// This is a deterministic implementation that parses contract content using simple patterns.
/// It does NOT call external AI APIs (Claude, OpenAI, etc.).
///
/// For production, implement with actual AI API integration in Infrastructure layer.
/// </summary>
public class MockAIContractAssistantService : IAIContractAssistantService
{
    /// <summary>
    /// Extract key information from contract content using pattern matching.
    /// Returns null for fields that cannot be extracted.
    /// </summary>
    public async Task<ExtractedContractInfoDto> ExtractContractInfoAsync(
        ExtractContractRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            throw new ArgumentException("ContractId must be non-empty.", nameof(request.ContractId));

        // Simulate async operation
        await Task.Delay(10, cancellationToken);

        var content = request.ContractContent ?? string.Empty;
        var extracted = new ExtractedContractInfoDto();

        // Extract Contract Number
        extracted.ContractNumber = ExtractField(content, @"(?:Contract\s+Number|HD|Số\s+HĐ)[\s:]*([A-Z0-9\-]+)", 1);

        // Extract Title
        extracted.Title = ExtractField(content, @"(?:Title|Tiêu\s+đề|Tên\s+HĐ)[\s:]*(.+?)(?:\n|$)", 1);

        // Extract Partner
        extracted.Partner = ExtractField(content, @"(?:Partner|Đối\s+tác|Bên\s+B)[\s:]*(.+?)(?:\n|$)", 1);

        // Extract Contract Type
        extracted.ContractType = ExtractField(content, @"(?:Contract\s+Type|Loại\s+HĐ)[\s:]*(.+?)(?:\n|$)", 1);

        // Extract Value (as decimal)
        var valueStr = ExtractField(content, @"(?:Value|Giá\s+trị)[\s:]*([0-9]+(?:,[0-9]{3})*(?:\.[0-9]{2})?)", 1);
        if (!string.IsNullOrEmpty(valueStr))
        {
            // Remove separators and parse
            valueStr = Regex.Replace(valueStr, @"[,\s]", "");
            if (decimal.TryParse(valueStr, CultureInfo.InvariantCulture, out var value))
            {
                extracted.Value = value;
            }
        }

        // Extract Signed Date
        extracted.SignedDate = ExtractDateField(content, @"(?:Signed|Ký)\s+(?:Date|ngày)[\s:]*");

        // Extract Effective Date
        extracted.EffectiveDate = ExtractDateField(content, @"(?:Effective|Có\s+hiệu\s+lực)\s+(?:Date|ngày)[\s:]*");

        // Extract Expiry Date
        extracted.ExpiryDate = ExtractDateField(content, @"(?:Expiry|Hết\s+hạn|Expir)\s+(?:Date|ngày)[\s:]*");

        // Extract Status
        extracted.Status = ExtractField(content, @"(?:Status|Trạng\s+thái)[\s:]*(.+?)(?:\n|$)", 1);

        return extracted;
    }

    /// <summary>
    /// Generate a summary of the contract using simple text processing.
    /// </summary>
    public async Task<ContractSummaryDto> SummarizeContractAsync(
        SummarizeContractRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            throw new ArgumentException("ContractId must be non-empty.", nameof(request.ContractId));

        if (string.IsNullOrWhiteSpace(request.ContractContent))
            throw new ArgumentException("ContractContent cannot be empty.", nameof(request.ContractContent));

        // Simulate async operation
        await Task.Delay(20, cancellationToken);

        var content = request.ContractContent.Trim();
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        // Create summary: take first meaningful line(s) and limit length
        var summaryBuilder = new System.Text.StringBuilder();
        foreach (var line in lines.Take(5))
        {
            if (line.Length > 10) // Skip very short lines
            {
                summaryBuilder.AppendLine(line);
                if (summaryBuilder.Length > 200)
                    break;
            }
        }

        var summary = summaryBuilder.ToString().Trim();
        if (summary.Length > 500)
            summary = summary.Substring(0, 497) + "...";

        // Extract key points from content
        var keyPoints = ExtractKeyPoints(content);

        return new ContractSummaryDto
        {
            ContractId = request.ContractId,
            Summary = !string.IsNullOrEmpty(summary) ? summary : "Contract summary generated from content.",
            KeyPoints = keyPoints
        };
    }

    /// <summary>
    /// Analyze contract for risks using simple rule-based detection.
    /// </summary>
    public async Task<ContractRiskAnalysisDto> AnalyzeContractRiskAsync(
        AnalyzeContractRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            throw new ArgumentException("ContractId must be non-empty.", nameof(request.ContractId));

        if (string.IsNullOrWhiteSpace(request.ContractContent))
            throw new ArgumentException("ContractContent cannot be empty.", nameof(request.ContractContent));

        // Simulate async operation
        await Task.Delay(15, cancellationToken);

        var content = request.ContractContent.ToLowerInvariant();
        var risks = new List<ContractRiskDto>();

        // Rule 1: Check for expiry/termination keywords
        if (Regex.IsMatch(content, @"\b(expir|expires|expiration|hết\s+hạn|terminate|termination|kết\s+thúc)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Contract Expiration/Termination",
                Description = "Contract contains expiration or termination clauses. Ensure renewal or termination procedures are planned.",
                Level = RiskLevel.Medium
            });
        }

        // Rule 2: Check for penalty/liability keywords
        if (Regex.IsMatch(content, @"\b(penalt|liability|fine|phạt|bồi\s+thường)\w*\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Penalty and Liability Clauses",
                Description = "Contract contains penalty or liability clauses. Review the conditions and amounts carefully.",
                Level = RiskLevel.High
            });
        }

        // Rule 3: Check for amendment/modification keywords
        if (Regex.IsMatch(content, @"\b(amendment|modification|change|amend|sửa\s+đổi)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Amendment and Modification Terms",
                Description = "Contract includes amendment or modification clauses. Verify the conditions and approval process.",
                Level = RiskLevel.Medium
            });
        }

        // Rule 4: Check for force majeure keywords
        if (Regex.IsMatch(content, @"\b(force\s+majeure|unforeseen|bất\s+khả\s+kháng)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Force Majeure Clause",
                Description = "Contract includes force majeure provisions. Understand the scope and implications for business continuity.",
                Level = RiskLevel.Low
            });
        }

        // Rule 5: Check for confidentiality keywords
        if (Regex.IsMatch(content, @"\b(confidential|nda|non-disclosure|bí\s+mật)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Confidentiality and NDA Terms",
                Description = "Contract includes confidentiality or NDA provisions. Ensure compliance with data protection requirements.",
                Level = RiskLevel.Medium
            });
        }

        // Rule 6: Check for payment terms keywords
        if (Regex.IsMatch(content, @"\b(payment|late\s+fee|interest|thanh\s+toán|trễ\s+hạn)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Payment Terms and Conditions",
                Description = "Contract specifies payment terms. Review payment schedule, methods, and late payment penalties.",
                Level = RiskLevel.Medium
            });
        }

        // Rule 7: Check for dispute resolution keywords
        if (Regex.IsMatch(content, @"\b(dispute|arbitration|litigation|court|tranh\s+chấp|tòa\s+án)\b", RegexOptions.IgnoreCase))
        {
            risks.Add(new ContractRiskDto
            {
                Title = "Dispute Resolution and Legal Venue",
                Description = "Contract defines dispute resolution procedures. Verify jurisdiction, arbitration terms, and legal venue.",
                Level = RiskLevel.High
            });
        }

        return new ContractRiskAnalysisDto
        {
            ContractId = request.ContractId,
            Risks = risks
        };
    }

    /// <summary>
    /// Extract a field value from content using regex pattern.
    /// Returns null if not found or empty.
    /// </summary>
    private static string? ExtractField(string content, string pattern, int groupIndex)
    {
        try
        {
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > groupIndex)
            {
                var value = match.Groups[groupIndex].Value.Trim();
                return !string.IsNullOrEmpty(value) ? value : null;
            }
        }
        catch
        {
            // Silently ignore regex errors
        }

        return null;
    }

    /// <summary>
    /// Extract a date field from content.
    /// Supports formats: DD-MM-YYYY, DD/MM/YYYY, YYYY-MM-DD, etc.
    /// </summary>
    private static DateTime? ExtractDateField(string content, string patternPrefix)
    {
        try
        {
            // Match date patterns after prefix
            var fullPattern = patternPrefix + @"(\d{1,4}[-/]\d{1,2}[-/]\d{1,4}|\d{1,2}[-/]\d{1,2}[-/]\d{4})";
            var match = Regex.Match(content, fullPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (match.Success && match.Groups.Count > 1)
            {
                var dateStr = match.Groups[1].Value.Trim();
                // Try common formats
                var formats = new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy" };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        return date;
                    }
                }
            }
        }
        catch
        {
            // Silently ignore parsing errors
        }

        return null;
    }

    /// <summary>
    /// Extract key points from contract content.
    /// </summary>
    private static List<string> ExtractKeyPoints(string content)
    {
        var keyPoints = new List<string>();

        // Split by lines and extract meaningful lines
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && l.Length > 15)
            .Take(10)
            .ToList();

        // Add up to 5 meaningful key points
        foreach (var line in lines.Take(5))
        {
            if (!line.All(char.IsDigit) && line.Length < 150) // Skip numeric-only or very long lines
            {
                keyPoints.Add(line);
            }
        }

        // If no key points found, create default ones
        if (!keyPoints.Any())
        {
            keyPoints.Add("Contract analysis provided. Review all terms and conditions carefully.");
        }

        return keyPoints;
    }
}
