import React from 'react';
import useSWR from 'swr';
import { Link } from 'react-router-dom';
import styled from 'styled-components';
import { PAGE_PATHS } from '../constants';

function getRecentCalendars(data: ICalDto, count: number = 5) {
  return Object.values(data.data)
    .sort((a, b) => b.updated_at.localeCompare(a.updated_at))
    .slice(0, count);
}

function RecentCalendars() {
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
          <li key={cal.path}>
            <Link to={`${PAGE_PATHS.CALENDAR}/${cal.source}`}>
              {cal.name} {Date.parse(cal.updated_at)}
            </Link>
            <a href={cal.path}>raw</a>
          </li>
        ))}
      </ul>
    </div>
  );
}

const Header = styled.header`
  background: slategray;
  display: flex;
  align-items: center;
  justify-content: center;
`;

const Brand = styled.h1`
  text-align: center;
`;

export default () => (
  <>
    <Header>
      <Brand>대학 달력</Brand>
    </Header>
    <RecentCalendars />
  </>
);
