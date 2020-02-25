import React from 'react';
import { useParams } from 'react-router-dom';

type RouteParams = {
  id: string;
};

export default () => {
  const params = useParams<RouteParams>();

  return <p>{params.id}</p>;
};
