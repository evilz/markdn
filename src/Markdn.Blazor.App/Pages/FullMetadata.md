---
url: /full-metadata
title: Full Metadata Test Page
$namespace: Markdn.Blazor.App.Custom
$using:
  - System.Text.Json
  - System.Linq
$inherit: Microsoft.AspNetCore.Components.ComponentBase
$parameters:
  - name: PostId
    type: int
  - name: Title
    type: string
  - name: ShowDetails
    type: bool
---

# Full Metadata Test

This page tests all YAML front matter configuration options.

## Parameters

- **PostId**: @PostId
- **Title**: @Title
- **ShowDetails**: @ShowDetails

## Current Time

The current time is: @DateTime.Now.ToString("HH:mm:ss")

@code {
    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(Title))
        {
            Title = "Default Title";
            ShowDetails = true;
        }
    }
}
