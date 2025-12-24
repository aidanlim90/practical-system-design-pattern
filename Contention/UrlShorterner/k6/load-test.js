import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    batch1: {
      executor: 'per-vu-iterations',
      vus: 1000,       // 3000 users
      iterations: 1,   // each user calls once
      startTime: '0s',
    },
    // batch2: {
    //   executor: 'per-vu-iterations',
    //   vus: 1000,       // 3000 users
    //   iterations: 1,
    //   startTime: '10s', // start after batch1
    // },
    // batch3: {
    //   executor: 'per-vu-iterations',
    //   vus: 2000,       // 4000 users
    //   iterations: 1,
    //   startTime: '20s', // start after batch2
    // },
  },
};

export default function () {
  const url = 'http://localhost:5168/urls'; // replace with your API endpoint
  const payload = JSON.stringify({
    LongUrl: `https://example.com/${__VU}-${Date.now()}`,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const res = http.post(url, payload, params);

  check(res, {
    'status is 200': (r) => r.status === 200,
  });
}
