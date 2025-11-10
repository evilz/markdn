---
$parameters:
  - name: Name
    type: string
  - name: ShowDetails
    type: bool
---

# Hello, @Name!

This is a personalized greeting for **@Name**.

Greeting generated at: @DateTime.Now.ToString("HH:mm:ss")

@code {
    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(Name))
        {
            Name = "Guest";
        }
    }
}
