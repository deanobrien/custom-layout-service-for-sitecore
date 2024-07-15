# Consume Layout Service for Sitecore

![enter image description here](https://deanobrien.uk/wp-content/uploads/2024/07/wireframe.jpg)


This repository is the companion code to support this article [DeanOBrien: Custom Layout Service for Sitecore](https://deanobrien.uk/creating-a-headless-application-without-the-sitecore-headless-services-module/) where we look at creating a custom layout service for sitecore.

To install this module:
- clone the repository
- compile the solution
- copy the DLL into your sitecore BIN folder
- copy config files into App_Config folder
- 
Call the endpoint: https://<insert-your-domain>/sitecore/api/layoutservice/get?site=website&lang=en&apiKey=xxx&path=/
