# PublisherStudio 4.8.8

- PublisherGeneratedMediaService read OrganicPlugins options through an unbound IOptionsMonitor, so MaximumMessageBytes/MaximumGeneratedMediaBytes ignored appsettings.json and could break generated-media downloads from LocalGPT. It now uses the bound IOptions instance.
