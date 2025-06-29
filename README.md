<img src="https://raw.githubusercontent.com/Checkmarx/ci-cd-integrations/main/.images/PluginBanner.jpg">
<br />
<div align="center">

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![APACHE License][license-shield]][license-url]

</div>
<br />

<p align="center">
  <a href="https://github.com/Checkmarx/ast-visual-studio-extension">
    <img src="https://raw.githubusercontent.com/Checkmarx/ci-cd-integrations/main/.images/cx-icon-logo.png" alt="Logo" width="80" height="80" />
  </a>

<h3 align="center">CHECKMARX ONE VISUAL STUDIO PLUGIN</h3>

<p align="center">
    <br />
    <a href="https://docs.checkmarx.com/en/34965-68738-checkmarx-one-visual-studio-extension--plugin-.html"><strong>Explore the docs »</strong></a>
    <br />
    <a href="https://marketplace.visualstudio.com/items?itemName=checkmarx.astVisualStudioExtension"><strong>Marketplace »</strong></a>
    <br />
    <br />
    <a href="https://github.com/Checkmarx/ast-visual-studio-extension/issues/new">Report Bug</a>
    ·
    <a href="https://github.com/Checkmarx/ast-visual-studio-extension/issues/new">Request Feature</a>
</p>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#overview">Overview</a></li>
    <li><a href="#key-features">Key Features</a></li>
    <li><a href="#prerequisites">Prerequisites</a></li>
    <li><a href="#initial-setup">Initial Setup</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
	  <li><a href="#feedback">Feedback</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## Overview

Checkmarx continues to spearhead the shift-left approach to AppSec by bringing our powerful AppSec tools into your IDE. This empowers developers to identify vulnerabilities and remediate them **as they code**. The Checkmarx Visual Studio extension integrates seamlessly into your IDE, enabling you to access the full functionality of your Checkmarx One account (SAST, SCA, IaC Security) directly from your IDE.

You can run new scans, or import results from scans run in your Checkmarx One account. Checkmarx provides detailed info about each vulnerability, including remediation recommendations and examples of effective remediation. The plugin enables you to navigate from a vulnerability to the relevant source code, so that you can easily zero-in on the problematic code and start working on remediation.


## Key Features

-  Access the full power of Checkmarx One (SAST, SCA, and IaC Security) directly from your IDE
-  Run a new scan from your IDE even before committing the code, or import scan results from your Checkmarx One account
-  Provides actionable results. Navigate from results panel directly to the highlighted vulnerable code in the editor and get right down to work on the remediation.
-  Group and filter results
-  Triage results (by adjusting the severity and state and adding comments) directly from the Visual Studio console (currently supported for SAST and IaC Security)
-  Links to Codebashing lessons
-  AI Secure Coding Assistant (ASCA) - A lightweight scan engine that runs in the background while you work, enabling developers to identify and remediate secure coding best practice violations as they code.


## Prerequisites

-  You are running Visual Studio version 2022.

-  You have an **API key** for your Checkmarx One account. To create an     API key, see
[Generating an API Key](https://checkmarx.atlassian.net/wiki/spaces/AST/pages/5859574017/Generating+an+API+Key).
	> The following are the minimum required  [roles](https://docs.checkmarx.com/en/34965-68603-managing-roles.html "Managing Roles")  for running an end-to-end flow of scanning a project and viewing results via the CLI or plugins:
	> -   CxOne composite role  `ast-scanner`    
	> -   CxOne role  `view-policy-management`
	> -   IAM role  `default-roles`


## Initial Setup

1.  Verify that all prerequisites are in place.

2.  Install the **Checkmarx One** extension from Marketplace.

3.  Configure the extension settings as described [here](https://checkmarx.com/resource/documents/en/34965-68739-installing-and-setting-up-the-checkmarx-one-visual-studio-extension.html).


## Contribution

We appreciate feedback and contribution to the visual studio extension! Before you get started, please see the following:

- [Checkmarx contribution guidelines](docs/contributing.md)
- [Checkmarx Code of Conduct](docs/code_of_conduct.md)

<!-- LICENSE -->
## License
Distributed under the [Apache 2.0](LICENSE). See `LICENSE` for more information.

## Feedback
We’d love to hear your feedback! If you come across a bug or have a feature request, please let us know by submitting an issue in [GitHub Issues](https://github.com/Checkmarx/ast-visual-studio-extension/issues).

<!-- CONTACT -->
## Contact

Checkmarx - AST Integrations Team

Project Link: [https://github.com/Checkmarx/ast-visual-studio-extension](https://github.com/Checkmarx/ast-visual-studio-extension)

Find more integrations from our team [here](https://github.com/Checkmarx/ci-cd-integrations#checkmarx-ast-integrations)


© 2022 Checkmarx Ltd. All Rights Reserved.

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
[license-url]: https://github.com/Checkmarx/ast-visual-studio-extension/blob/main/LICENSE.txt
