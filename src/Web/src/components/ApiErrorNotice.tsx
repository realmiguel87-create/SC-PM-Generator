import { Card, CardContent } from "@/components/ui/card";
import { ApiError } from "@/lib/api-client";

/**
 * Reports why a data request actually failed, rather than asserting a cause it has not checked.
 *
 * This replaces a message that read "Could not reach the API. Start the SCPM.Api project to see
 * live data." shown on *any* query error. During a real setup session that message appeared while
 * the API was running perfectly and answering 401 because the user's session had lapsed — it sent
 * the debugging in entirely the wrong direction for a considerable time. An error message that
 * names a specific cause it has not verified is worse than one that simply reports what happened.
 *
 * The status now comes from ApiError rather than from a regex over the message. The regex version
 * worked only when the API sent no response body, because the status number was then present in
 * the client's fallback message; a ProblemDetails body replaced that message with its `title` and
 * the number vanished, quietly downgrading a 403 to the generic branch. That is the same mistake
 * as the message this component replaced — inferring a cause instead of reading one.
 */
export function ApiErrorNotice({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : String(error);

  // fetch() rejects with a TypeError ("Failed to fetch") when it cannot reach the server at all.
  // Anything else means the server answered, and ApiError carries the status it answered with.
  const isNetworkFailure = error instanceof TypeError;
  const status = error instanceof ApiError ? error.status : undefined;

  return (
    <Card>
      <CardContent className="pt-5 text-sm text-critical">
        {isNetworkFailure && (
          <>Could not reach the API. Check that the SCPM.Api project is running.</>
        )}
        {status === 401 && (
          <>Not signed in, or your session has expired. Use Sign in on the left to continue.</>
        )}
        {status === 403 && (
          <>
            Signed in, but your account does not have a role permitting this. An administrator
            needs to grant one.
          </>
        )}
        {!isNetworkFailure && status !== 401 && status !== 403 && (
          <>The request failed: {message}</>
        )}
      </CardContent>
    </Card>
  );
}
