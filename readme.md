# ARLO Project

## Overview
ARLO is a C# project that converts software requirements into architectural decisions. Utilizing GPT-4 for parsing and an ILP-based optimizer for decision-making streamlines the architectural design process.

## Key Components
- **RequirementParser**: Parses natural language requirements into ASRs.
- **Architect**: Develops architectural policies from ASRs.

## How to Use
1. **Clone the Repository**: Access the code on GitHub.
2. **Register your OpenAI Key**: See below.
3. **Install Dependencies**: Follow the setup instructions for necessary libraries and tools.
4. **Execute `CategorizedReqsExperiment`**: Configure and run the experiment with your system preferences.
5. **Review Output**: Analyze the generated architectural decisions.

## Contributing
Contributions are welcome! Please take a look at our contributing guidelines for more information.

### Setting Up Your OpenAI API Key on macOS

To use your OpenAI API key in a C# application, follow these steps to set up the environment variable on your Mac:

1. **Open Terminal**: Open the Terminal application on your Mac.

2. **Edit Shell Configuration File**:
    - Identify the shell you are using by running:
      ```sh
      echo $SHELL
      ```
    - If you are using `bash` (default in many macOS versions), open the appropriate file:
      ```sh
      nano ~/.bash_profile
      ```
      or
      ```sh
      nano ~/.bashrc
      ```
    - If you are using `zsh` (default in macOS Catalina and later), open:
      ```sh
      nano ~/.zshrc
      ```

3. **Set the Environment Variable**:
    - Add the following line to the configuration file you opened:
      ```sh
      export GptApiKey="your_openai_api_key_here"
      ```
    - Replace `"your_openai_api_key_here"` with your actual OpenAI API key.

4. **Save and Close the File**:
    - If you are using `nano`, save the file by pressing `CTRL + O`, then press `Enter` to confirm. Exit by pressing `CTRL + X`.

5. **Apply the Changes**:
    - Run the following command in the Terminal to apply the changes:
      ```sh
      source ~/.bash_profile
      ```
      or
      ```sh
      source ~/.bashrc
      ```
      or
      ```sh
      source ~/.zshrc
      ```

6. **Verify the Environment Variable**:
    - Verify that the environment variable is set correctly by running:
      ```sh
      echo $GptApiKey
      ```
    - This should print your OpenAI API key to the terminal.

Now, when you run your C# application, the environment variable `GptApiKey` should be available, and you can retrieve it using:

```csharp
string apiKey = Environment.GetEnvironmentVariable("GptApiKey");
