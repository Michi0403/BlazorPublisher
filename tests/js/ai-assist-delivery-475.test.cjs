'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const web = relative => fs.readFileSync(path.join(root, 'src/PublisherStudio.Web', relative), 'utf8');

test('4.7.5 tracks every AI Assist request and delivers a finished Council result without a proposal call', () => {
    const page = web('Components/Pages/OrganicPlugins.razor');
    assert.match(page, /@inject IAiAssistDeliveryTracker AiAssistDelivery/);
    const generate = page.slice(page.indexOf('private async Task GenerateTextProposalAsync'), page.indexOf('private void ShowAiAssistDeliveryNotices'));
    assert.ok(generate.indexOf('await Connection.SendCouncilRequestAsync(request)') < generate.indexOf('AiAssistDelivery.Track(id, insertionTarget, kind);'), 'tracking starts with the returned correlation id');
    assert.match(page, /AiAssistDelivery\.Changed \+= OnServiceChanged;/);
    assert.match(page, /AiAssistDelivery\.Changed -= OnServiceChanged;/);

    const tracker = web('Services/OrganicPlugins/AiAssistDeliveryTracker.cs');
    assert.match(tracker, /results\.Changed \+= OnResultsChanged;/);
    assert.match(tracker, /results\.Changed -= OnResultsChanged;/);
    // A proposal that already arrived, or one waiting for LocalGPT approval, is never duplicated by the fallback.
    const deliver = tracker.slice(tracker.indexOf('private void Deliver('), tracker.indexOf('private (string Deliverable, bool ProposalAwaitingApproval) ReadCouncilResult'));
    assert.ok(deliver.indexOf('if (tracking.ProposalSeen)') < deliver.indexOf('results.AddTextProposal('));
    assert.ok(deliver.indexOf('if (council.ProposalAwaitingApproval)') < deliver.indexOf('results.AddTextProposal('));
    assert.match(deliver, /Target = tracking\.Target/);
    // The result is claimed before delivery so re-entrant store events cannot deliver twice.
    assert.match(tracker, /tracking\.Completed = true; \/\/ claimed before delivery/);
    assert.match(tracker, /ReadString\(root, "DeliverableText"\)/);
    assert.match(tracker, /StartsWith\(ProposalFunctionName \+ ":"/);

    const registration = web('PublisherStudioServiceCollectionExtensions.cs');
    assert.match(registration, /AddSingleton<IAiAssistDeliveryTracker, AiAssistDeliveryTracker>\(services\);/);
});

test('4.7.5 the AI Assist page never calls browser storage from a prerendered instance', () => {
    const page = web('Components/Pages/OrganicPlugins.razor');
    const changed = page.slice(page.indexOf('private void OnServiceChanged()'), page.indexOf('private async Task SaveAutoConnectPreferenceAsync'));
    assert.match(changed, /if \(_automationStateLoaded && Connection\.State\.IsLinked/);
    assert.match(changed, /catch \(InvalidOperationException exception\)/);
    assert.match(changed, /ShowAiAssistDeliveryNotices\(\);/);
});
