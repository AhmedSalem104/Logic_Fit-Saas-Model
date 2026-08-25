export type EnvironmentName = 'development' | 'test' | 'staging' | 'production';

export type ApiMeta = {
  requestId: string;
  version: string;
  page?: number;
  pageSize?: number;
  total?: number;
  hasNext?: boolean;
};

export type ApiSuccess<T> = { data: T; meta: ApiMeta };

export type ApiFieldError = { field: string; code: string };

export type ApiErrorBody = {
  error: {
    code: string;
    message: string;
    fieldErrors?: ApiFieldError[];
  };
  meta: ApiMeta;
};
