# PublisherStudio 4.7.5: AI Assist delivery and prerender guard

4.7.5 is 4.7.4 plus two fixes found in the 9 November 2026 logs. It pairs with LocalGPT 7.0.4, and older LocalGPT versions still work.

## Fixed

### AI Assist results no longer disappear

In the logs, AI Assist requests #12 (story) and #13 (spreadsheet) received their Council WorkResult, but nothing was inserted and no message appeared. The page only waited for a `publisher.text.proposal.request` call, and the Council never made that call.

New singleton `AiAssistDeliveryTracker` (`Services/OrganicPlugins/AiAssistDeliveryTracker.cs`, interface `IAiAssistDeliveryTracker.cs`):

- `OrganicPlugins.razor` registers each AI Assist request with its correlation id, target and kind right after sending it.
- When the final WorkResult or error for that correlation id arrives, there are four outcomes:
  - A proposal for the target already came through the proposal function. Nothing more happens.
  - LocalGPT's result shows a `publisher.text.proposal.request` that is still waiting for approval in LocalGPT. The tracker evaluates the `ApprovalGatedRequest` evidence for this. The page tells you to approve it in LocalGPT, and it is inserted afterwards. The tracker never inserts it a second time.
  - Otherwise the Council deliverable becomes a proposal for the tracked target:
    - It uses LocalGPT 7.0.4's `DeliverableText`, or for older versions `FinalAnswer` without the Keyaccount report.
    - The existing `PublisherAiDocumentInsertionService.TryApply` inserts it only if this circuit authorized that target, with undo history. Otherwise it stays as a reviewable proposal.
  - An error, or a result without a usable deliverable, becomes an error notification.
- A result can be delivered only once:
  - The tracker claims a result before delivering it.
  - `TryApply` removes the authorization after one insertion.
- Requests that receive no result are no longer tracked after 12 hours.

### No more JavaScript interop errors from the prerendered page

`OrganicPlugins.OnServiceChanged` called `localStorage.setItem` on the prerendered page instance. That caused five `InvalidOperationException: JavaScript interop calls cannot be issued at this time` entries. The call now runs only after the page's first interactive render, and the remaining `InvalidOperationException` is logged at Debug.

## Changed files

- `Services/OrganicPlugins/AiAssistDeliveryTracker.cs`, `Services/OrganicPlugins/IAiAssistDeliveryTracker.cs` (new)
- `BusinessObjects/AiAssistDeliveryNotice.cs`, `BusinessObjects/AiAssistDeliveryTracking.cs` (new)
- `Components/Pages/OrganicPlugins.razor`: tracking, delivery notices, prerender guard
- `PublisherStudioServiceCollectionExtensions.cs`: singleton registration
- `tests/js/ai-assist-delivery-475.test.cjs` (new)
- Both project files are at version 4.7.5.

## Verification status

- `tests/js/ai-assist-delivery-475.test.cjs` and all existing `tests/js` files pass under Node, run file by file.
- No compiler run in this environment. The build and runtime checks below are still open.

## Runtime checks

1. Run "Generate and insert" with a writing prompt.
   - If LocalGPT shows a pending `publisher.text.proposal.request` approval, PublisherStudio says so. After you approve it in LocalGPT, the text is inserted once.
   - If no proposal is made, the finished result is inserted once, with undo history.
2. Repeat with a spreadsheet prompt. CSV output is inserted through the existing spreadsheet path.
3. Stop LocalGPT during a request. The error notification appears when the error result arrives.
4. Reload `/organic-plugins` while linked. The log contains no `JavaScript interop calls cannot be issued` errors.

## Interaction / frontend release checklist

- No object, editor mode, toolbar, overlay, renderer, stacking context or input routing changed.
- Frontend failure boundary: new notices go through the existing `IUserNotificationService`, and every new component method logs its failures.
