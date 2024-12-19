import { FC } from "react";
import { Link } from "@nextui-org/link";

import { UserModel } from "@/api/types";
import { Flag } from "@/components/flag";

type Props = { user: UserModel };

export const User: FC<Props> = (props) => {
  const user = props.user;

  return (
    <div className="flex py-1 gap-1">
      <Link href={`/leaderboard?country=${user.countryCode}&page=1`}>
        <Flag country={user.countryCode} width={20} />
      </Link>
      <Link className="text-primary-500" href={`/users/${user.id}`} size="sm">
        {user.username}
      </Link>
    </div>
  );
};
