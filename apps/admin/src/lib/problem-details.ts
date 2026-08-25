export interface ProblemDetails {
  status: number;
  detail: string;
  code: string;
  traceId: string;
}

export class ProblemDetailsError extends Error {
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail);
    this.name = "ProblemDetailsError";
    this.problem = problem;
  }
}

export function isProblemDetailsError(
  error: unknown,
): error is ProblemDetailsError {
  return error instanceof ProblemDetailsError;
}

export function hasCode(error: unknown, code: string): boolean {
  return isProblemDetailsError(error) && error.problem.code === code;
}

export function isClientError(error: unknown): boolean {
  return (
    isProblemDetailsError(error) &&
    error.problem.status >= 400 &&
    error.problem.status < 500
  );
}
