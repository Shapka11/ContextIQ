import { useInfiniteQuery } from '@tanstack/react-query'
import { historyApi } from '../api'
export function useHistory() {
  return useInfiniteQuery({
    queryKey: ['history'],
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) => historyApi.page(pageParam, 100, signal),
    getNextPageParam: (last) =>
      last.page * last.pageSize < last.total ? last.page + 1 : undefined,
  })
}
