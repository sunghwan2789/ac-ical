import React from 'react';
import useSWR from 'swr';
import { Link } from 'react-router-dom';
import { PAGE_PATHS } from '~/constants';

type ICalDto = {
  updated_at: string;
  data: {
    [id: string]: {
      name: string;
      path: string;
      source: string;
      updated_at: string;
    };
  };
};

function getRecentCalendars(data: ICalDto, count: number = 5) {
  return Object.values(data.data)
    .sort((a, b) => b.updated_at.localeCompare(a.updated_at))
    .slice(0, count);
}

export default () => {
  const { data, error } = useSWR<ICalDto>('ical.json');

  if (error) {
    return <div>{error}</div>;
  }

  if (!data) {
    return <div>no data</div>;
  }

  return (
    <div>
      <p>last update: {data.updated_at}</p>
      <ul>
        {getRecentCalendars(data).map(cal => (
          <li>
            <Link to={`${PAGE_PATHS.CALENDAR}/${cal.source}`}>
              {cal.name} {Date.parse(cal.updated_at)}
            </Link>
            <a href={cal.path}>raw</a>
          </li>
        ))}
      </ul>
    </div>
  );
};
