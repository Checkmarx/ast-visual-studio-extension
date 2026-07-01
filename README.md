

<img src="https://raw.githubusercontent.com/Checkmarx/ci-cd-integrations/main/.images/PluginBanner.jpg">
<br />
<div align="center">

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![Install][install-shield]][install-url]
[![License][license-shield]][license-url]

</div>

<br />
<p align="center">
  <a href="https://github.com/Checkmarx/ast-visual-studio-extension">
    <img src="https://raw.githubusercontent.com/Checkmarx/ci-cd-integrations/main/.images/cx_x_icon.png" alt="Logo" width="80" height="80" />
  </a>
  <h3 align="center">CHECKMARX VISUAL STUDIO PLUGIN</h3>
  <p align="center">
    <a href="https://docs.checkmarx.com/en/34965-68739-installing-and-setting-up-the-checkmarx-one-visual-studio-extension.html"><strong>Explore the docs »</strong></a>
    <br />
    <a href="https://marketplace.visualstudio.com/items?itemName=checkmarx.astVisualStudioExtension"><strong>Marketplace »</strong></a>
  </p>
</p>
<p  align="center">
The Checkmarx Visual Studio plugin enables you to import results from a Checkmarx One scan directly into your IDE and run new scans from the IDE.
</p>



<br  />
<p  align="center">
<a  href="https://github.com/Checkmarx/ast-visual-studio-extension/issues/new">Report Bug</a>
·
<a  href="https://github.com/Checkmarx/ast-visual-studio-extension/issues/new">Request Feature</a>

</p>
<br>
  
  
  

<!-- TABLE OF CONTENTS -->

<details>

<summary>Table of Contents</summary>

<ol>

<li><a  href="#overview">Overview</a></li>

<li><a href="#checkmarx-one-platform">Checkmarx One Platform</a></li>

<li><a href="#checkmarx-developer-assist">Checkmarx Developer Assist</a></li>

<li><a  href="#feedback">Feedback</a></li>

<li><a  href="#contribution">Contribution</a></li>

<li><a  href="#license">License</a></li>

<li><a  href="#contact">Contact</a></li>

</ol>

</details>

  
  
  
  

# Overview

  

Checkmarx continues to spearhead the shift-left approach to AppSec by bringing our powerful AppSec tools into your IDE. This empowers developers to identify vulnerabilities and remediate them **as they code**. The Checkmarx Visual Studio plugin integrates seamlessly into your IDE, identifying vulnerabilities in your proprietary code, open source dependencies, and IaC files. The plugin offers actionable remediation insights in real-time.

The Checkmarx Visual Studio extension contains two separate capabilities:

-   Checkmarx One Platform

-   Checkmarx Developer Assist  


## Checkmarx One Platform

This tool enables Checkmarx One users to access the full functionality of your Checkmarx One account (SAST, SCA, IaC, and Secret Detection) directly from your IDE. You can run new scans or import results from scans run in your Checkmarx One account. Checkmarx provides detailed info about each vulnerability, including remediation recommendations and examples of effective remediation. The plugin enables you to navigate from a vulnerability to the relevant source code, so that you can easily zero-in on the problematic code and start working on remediation. <br>

### Key Features  

- Access the full power of Checkmarx One (SAST, SCA, IaC Security, API Security, Container Security) directly from your IDE.  

- Run a new scan from your IDE even before committing the code, or import scan results from your Checkmarx One account.  

- Rescan an existing branch from your IDE or create a new branch in Checkmarx One for the local branch in your workspace. 

- Provides actionable results including remediation recommendations. Navigate from results panel directly to the highlighted vulnerable code in the editor and get right down to work on the remediation. 

- Authenticate with Checkmarx via API Key.  

- Group and filter results.  

- Triage results (by adjusting the severity and state and adding comments) directly from the Visual Studio console (currently supported for SAST and IaC Security).  

- Apply Auto Remediation to automatically remediate open source vulnerabilities, by updating to a non-vulnerable package version.  

- Links to Codebashing lessons.

---  

### Prerequisites  

  - Supported for **Visual Studio 2022** and **Visual Studio 2026** - Community, Professional and Enterprise editions
  - Officially supported only for .NET Framework version 4.7.2, 4.8 or 4.8.1.
  - You have an **API key** for your Checkmarx One account.  See [Generating an API Key](https://checkmarx.com/resource/documents/en/34965-68618-generating-an-api-key.html)
> 🔑 In order to use this integration for running an end-to-end flow of scanning a project and viewing results with the minimum required permissions, the API Key or user account should have the role `plugin-scanner`. Alternatively, they can have at a minimum the out-of-the-box composite role `ast-scanner` as well as the IAM role `default-roles`.

### Initial Setup

- Verify that all prerequisites are in place.  

- Install the **Checkmarx** plugin and configure the settings as
described [here](https://docs.checkmarx.com/en/34965-68739-installing-and-setting-up-the-checkmarx-one-visual-studio-extension.html)

### Usage

* Learn about using this plugin [here](https://docs.checkmarx.com/en/34965-68739-installing-and-setting-up-the-checkmarx-one-visual-studio-extension.html)

## Checkmarx Developer Assist
Developer Assist is an agentic AI tool that delivers real-time context-aware prevention, remediation, and guidance to developers inside the IDE. 
<br>

### Key Features
- An advanced security agent that delivers real-time context-aware prevention, remediation, and guidance to developers from the IDE.
- Realtime scanners identify risks as you code.
  - AI Secure Coding Assistant (ASCA), a lightweight source code scanner, enables developers to identify secure coding best practice violations in the file that they are working on as they code.
  - Specialized realtime scanners identify vulnerable open source packages and container images, as well as exposed secrets and IaC risks.
- MCP-based agentic AI remediation.
- AI powered explanation of risk details.

### Prerequisites
  - Supported for **Visual Studio 2022** version 17.0+ and **Visual Studio 2026** - Community, Professional and Enterprise editions
 - A Checkmarx One account with a **Checkmarx One Assist** or **AI protection** license, and with the **Checkmarx MCP** activated for your tenant account in the Checkmarx One UI under **Settings → Plugins**. This must be done by an account admin.
 - You have **GitHub Copilot** installed and running

### Installation
1. Install the **Checkmarx Developer Assist** extension from the Visual Studio Marketplace.
2. In the IDE, open Checkmarx Developer Assist **Settings**, click on **Authentication**, and enter your activation key in the **Developer Assist API Key** field.
3. Start running the Checkmarx MCP server.
4. Enable the MCP tools by going to **Tools** → **GitHub Copilot** → **Copilot Chat** and selecting the checkbox next to each of the three Checkmarx tools.
5. Optionally, adjust Checkmarx Developer Assist settings.
6. For **Visual Studio 2022** we recommend setting GitHub Copilot Chat to **Agent mode** in order to streamline the workflow. 

### Usage
**GIF - AI Remediation with Developer Assist**
![AI Remediation with Developer Assist](https://raw.githubusercontent.com/Checkmarx/ci-cd-integrations/main/.images/Visual_Studio_AI_Remediation_with_Developer_Assist.gif "Running a Scan from the IDE")
* Learn about using Checkmarx Developer Assist [here](https://docs.checkmarx.com/en/34965-405960-checkmarx-developer-assist.html)


## Feedback

We’d love to hear your feedback! If you come across a bug or have a feature request, please let us know by submitting an issue in [GitHub Issues](https://github.com/Checkmarx/ast-visual-studio-extension/issues).

  
## Contribution

  

We appreciate feedback and contribution to the Visual Studio plugin! Before you get started, please see the following:
  

- [Checkmarx contribution guidelines](docs/contributing.md)

- [Checkmarx Code of Conduct](docs/code_of_conduct.md)

  

<!-- LICENSE -->

## License

Distributed under the [Apache 2.0](LICENSE). See `LICENSE` for more information.

  
  

<!-- CONTACT -->

## Contact

  

Checkmarx - Checkmarx Integrations Team

  

Project Link: [https://github.com/Checkmarx/ast-visual-studio-extension](https://github.com/Checkmarx/ast-visual-studio-extension)

  

Find more integrations from our team [here](https://github.com/Checkmarx/ci-cd-integrations#checkmarx-ast-integrations)

  
  

© 2026 Checkmarx Ltd. All Rights Reserved.

  

<!-- MARKDOWN LINKS & IMAGES -->

<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[contributors-shield]: https://img.shields.io/github/contributors/Checkmarx/ast-visual-studio-extension.svg

[contributors-url]: https://github.com/Checkmarx/ast-visual-studio-extension/graphs/contributors

[forks-shield]: https://img.shields.io/github/forks/Checkmarx/ast-visual-studio-extension.svg

[forks-url]: https://github.com/Checkmarx/ast-visual-studio-extension/network/members

[stars-shield]: https://img.shields.io/github/stars/Checkmarx/ast-visual-studio-extension.svg

[stars-url]: https://github.com/Checkmarx/ast-visual-studio-extension/stargazers

[issues-shield]: https://img.shields.io/github/issues/Checkmarx/ast-visual-studio-extension.svg

[issues-url]: https://github.com/Checkmarx/ast-visual-studio-extension/issues

[license-shield]: https://img.shields.io/github/license/Checkmarx/ast-visual-studio-extension.svg

[license-url]: https://github.com/Checkmarx/ast-visual-studio-extension/blob/main/LICENSE

[install-shield]: https://vsmarketplacebadges.dev/installs-short/checkmarx.astVisualStudioExtension.svg

[install-url]: https://marketplace.visualstudio.com/items?itemName=checkmarx.astVisualStudioExtension