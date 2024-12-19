import { FC } from "react";
import { Spacer } from "@nextui-org/spacer";
import { Link } from "@nextui-org/link";

import { UserModel } from "@/api/types";
import { Flag } from "@/components/flag";

type Props = { user: UserModel };

export const User: FC<Props> = (props) => {
  const user = props.user;

  return (
    <Link className="text-primary-500" href={`/users/${user.id}`} size="sm">
      <Flag country={user.countryCode} width={20} />
      <Spacer x={1} />
      {user.username}
    </Link>
  );
};
