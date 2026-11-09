'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const source = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const web = relative => source(`src/PublisherStudio.Web/${relative}`);

test('4.7.3 journals every TCP 1-Wire packet in both directions, rejected frames included', () => {
    const connection = web('Services/OrganicPlugins/LocalGptConnectionService.cs');
    assert.match(connection, /IOrganicWireTrafficJournal traffic,/);
    // Outgoing: plaintext captured before protection, frame journaled after the write and on failure.
    const send = connection.slice(connection.indexOf('private async Task<Guid> SendEnvelopeCoreAsync'), connection.indexOf('private void JournalTcp'));
    assert.ok(send.indexOf('plaintext = traffic.CapturePlaintext(envelope);') < send.indexOf('await security.ProtectOutgoingAsync(envelope'), 'plaintext must be captured before encryption');
    assert.match(send, /JournalTcp\(OrganicWireTrafficDirections\.Sent, targetPeerId, json, envelope, plaintext, null\);/);
    assert.match(send, /JournalTcp\(OrganicWireTrafficDirections\.Sent, targetPeerId, json, envelope, plaintext, ex\.Message\);/);
    // Incoming: the existing decrypt gate stays, rejected lines are journaled before the loop stops.
    const read = connection.slice(connection.indexOf('private async Task ReadLoopAsync'), connection.indexOf('private async Task HandleIncomingAsync'));
    assert.match(read, /await security\.UnprotectIncomingAsync\(envelope, cancellationToken\)/);
    assert.match(read, /JournalTcp\(OrganicWireTrafficDirections\.Received, connectedPeerId, line, envelope, string\.Empty, rejection\.Message\);\s*throw;/);
    assert.match(read, /JournalTcp\(OrganicWireTrafficDirections\.Received, connectedPeerId, line, accepted, string\.Empty, null\);/);
    assert.ok(read.indexOf('JournalTcp(OrganicWireTrafficDirections.Received, connectedPeerId, line, accepted') < read.indexOf('await HandleIncomingAsync(accepted'), 'accepted packets are journaled before dispatch');
    // Manual connect by URL: the profile request and its body are journaled, parsed from the same text.
    assert.match(connection, /JournalProfile\(OrganicWireTrafficDirections\.Sent, profileEndpoint/);
    assert.match(connection, /JournalProfile\(\s*OrganicWireTrafficDirections\.Received,\s*profileEndpoint,\s*profileBody/);
    assert.match(connection, /JsonSerializer\.Deserialize<LocalGptWebConnectionProfile>\(profileBody, codec\.JsonOptions\)/);
});

test('4.7.3 journals HTTP/JSON and discovery traffic', () => {
    const controller = web('Controllers/OrganicWireHttpController.cs');
    assert.match(controller, /IOrganicWireTrafficJournal traffic,/);
    assert.match(controller, /private async Task<IActionResult> RespondAsync\(OrganicWireEnvelope response/);
    assert.doesNotMatch(controller, /return Content\(codec\.Serialize\(response\)/, 'every response goes through the journaling RespondAsync');
    assert.match(controller, /if \(!requestJournaled\)\s*JournalHttp\(OrganicWireTrafficDirections\.Received, json, received, string\.Empty, ex\.Message\);/);
    assert.match(controller, /JournalProfile\(profile\);/);

    const discovery = web('HostedServices/OrganicPlugins/LocalGptDiscoveryHostedService.cs');
    assert.match(discovery, /IOrganicWireTrafficJournal traffic,/);
    assert.match(discovery, /JournalBeacon\(received, string\.Empty, \$"Ignored: oversized discovery datagram/);
    assert.match(discovery, /JournalBeacon\(received, peer\.PeerId, string\.Empty\);/);
    assert.match(discovery, /JournalBeacon\(received, string\.Empty, \$"Ignored: malformed discovery data/);
});

test('4.7.3 traffic settings are live, logged per level and shown in the app', () => {
    const settings = JSON.parse(web('appsettings.json').replace(/^﻿/, ''));
    assert.equal(settings.OrganicPlugins.Traffic.Enabled, true);
    assert.equal(settings.OrganicPlugins.Traffic.PayloadMode, 'Full');
    assert.equal(settings.Logging.LogLevel['PublisherStudio.Services.OrganicPlugins.OrganicWireTrafficJournal'], 'Information');

    const registration = web('PublisherStudioServiceCollectionExtensions.cs');
    assert.match(registration, /AddOptions<OrganicWireTrafficOptions>\(\)\s*\.Bind\(configuration\.GetSection\(OrganicWireTrafficOptions\.SectionName\)\)/);
    assert.match(registration, /AddSingleton<IOrganicWireTrafficJournal, OrganicWireTrafficJournal>\(services\);/);

    const journal = web('Services/OrganicPlugins/OrganicWireTrafficJournal.cs');
    assert.match(journal, /IOptionsMonitor<OrganicWireTrafficOptions> options/);
    assert.match(journal, /logger\.LogDebug\(\s*"1-Wire #\{Sequence\} \{Direction\} payload/);
    assert.match(journal, /logger\.LogTrace\(\s*"1-Wire #\{Sequence\} \{Direction\} raw frame/);

    const page = web('Components/Pages/OrganicPlugins.razor');
    assert.match(page, /<OrganicWireTrafficPanel \/>/);
    const panel = web('Components/OrganicPlugins/OrganicWireTrafficPanel.razor');
    assert.match(panel, /@inject ILogger<OrganicWireTrafficPanel> Logger/);
    assert.match(panel, /Traffic\.Changed -= OnTrafficChanged;/);
    assert.match(panel, /Notifications\.Error\(/);
    assert.doesNotMatch(panel, /<\s*(?:section|dialog|details|button|input|select|textarea)\b/);
});
