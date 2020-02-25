type ICalDto = {
  updated_at: string;
  data: {
    [id: string]: ICalMetaDto;
  };
};

type ICalMetaDto = {
  name: string;
  path: string;
  source: string;
  updated_at: string;
};
