﻿namespace SubmissionCheckSplitter.Data.Config;

using System.ComponentModel.DataAnnotations;

public class SubmissionApiConfig
{
    public const string Section = "SubmissionApi";

    [Required]
    public string BaseUrl { get; set; }
}
