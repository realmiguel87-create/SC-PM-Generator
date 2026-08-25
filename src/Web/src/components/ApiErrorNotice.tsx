import { Card, CardContent } from "@/components/ui/card";

/**
 * Reports why a data request actually failed, rather than asserting a cause it has not checked.
 *
 * This replaces a message that read "Could not reach the API. Start the SCPM.Api project to see
 * live data." shown on *any* query error. During a real setup session that message appeared while
 * the API was running perfectly and answering 401 because the user's session had lapsed — it sent
 * the debugging in entirely the wrong direction for a considerable time. An error message that
 * names a specific cause it has not verified is worse than one that simply reports what happened.
 */
export function ApiErrorNotice({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : String(error);

  // fetch() rejects with a TypeError ("Failed to fetch") when it cannot reach the server at all.
  // Anything else means the server answered — with a status the api-client turned into an Error.
  const isNetworkFailure = error instanceof TypeError;
  const isUnauthorised = /\b401\b/.test(message);
  const isForbidden = /\b403\b/.test(message);

  return (
    <Card>
      <CardContent className="pt-5 text-sm text-critical">
        {isNetworkFailure && (
          <>Could not reach the API. Check that the SCPM.Api project is running.</>
        )}
        {isUnauthorised && (
          <>Not signed in, or your session has expired. Use Sign in on the left to continue.</>
        )}
        {isForbidden && (
          <>
            Signed in, but your account does not have a role permitting this. An administrator
            needs to grant one.
          </>
        )}
        {!isNetworkFailure && !isUnauthorised && !isForbidden && (
          <>The request failed: {message}</>
        )}
      </CardContent>
    </Card>
  );
}
